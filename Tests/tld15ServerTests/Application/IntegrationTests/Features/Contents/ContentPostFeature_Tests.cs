// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Exceptions;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Contents;
using tld15Server.Features.Shared.Business;

namespace tld15ServerTests.Application.IntegrationTests.Features.Contents;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class ContentPostFeature_Tests
{
    private const string _english = "en";
    private const string _japanese = "ja";

    private static ContentPostFeature.Handler CreatePostHandler(IServiceProvider provider)
    {
        return new ContentPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static ContentGetFeature.Handler CreateGetHandler(IServiceProvider provider)
    {
        return new ContentGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static Task<ContentGetFeature.Result> ReadAsync(IServiceProvider provider)
    {
        return CreateGetHandler(provider)
            .Handle(new ContentGetFeature.Query { Id = Globals.Content.Lore, LanguageId = _english }, CancellationToken.None)
            .AsTask();
    }

    [Fact]
    public async Task Handle_StoresTheBodyOfEveryLocale()
    {
        var provider = IntegrationTestSetup.GetServices();

        var stored = await ReadAsync(provider);

        await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
        {
            Id = stored.Id,
            VersionLocal = stored.VersionLocal,
            Markdown = { [_english] = "# English", [_japanese] = "# 日本語" },
        }, CancellationToken.None);

        var result = await ReadAsync(provider);

        Assert.Equal("# English", result.Markdown[_english]);
        Assert.Equal("# 日本語", result.Markdown[_japanese]);
        Assert.True(result.VersionLocal > stored.VersionLocal);
    }

    [Fact]
    public async Task Handle_ClearsTheLocale_WhenTheBodyIsBlank()
    {
        var provider = IntegrationTestSetup.GetServices();

        var stored = await ReadAsync(provider);

        await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
        {
            Id = stored.Id,
            VersionLocal = stored.VersionLocal,
            Markdown = { [_english] = "# Written", [_japanese] = "   " },
        }, CancellationToken.None);

        var result = await ReadAsync(provider);

        Assert.True(result.Markdown.ContainsKey(_english));
        Assert.False(result.Markdown.ContainsKey(_japanese));
    }

    [Fact]
    public async Task Handle_KeepsTheLinks_WhenTheEditorOnlySendsThemBack()
    {
        var provider = IntegrationTestSetup.GetServices();

        var stored = await ReadAsync(provider);

        // What the editor of the links stores.
        await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
        {
            Id = stored.Id,
            VersionLocal = stored.VersionLocal,
            Links = [new SharedContentLink
            {
                TranslationLanguageId = _english,
                Language = "en",
                Url = "https://example.org",
            }],
        }, CancellationToken.None);

        var withLink = await ReadAsync(provider);

        // What the editor of the body stores: its own text, and the links exactly as it read them.
        await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
        {
            Id = withLink.Id,
            VersionLocal = withLink.VersionLocal,
            Markdown = { [_english] = "# Body" },
            Links = withLink.Links,
        }, CancellationToken.None);

        var result = await ReadAsync(provider);

        Assert.Equal("# Body", result.Markdown[_english]);
        var link = Assert.Single(result.Links);
        Assert.Equal("https://example.org", link.Url);
    }

    [Fact]
    public async Task Handle_Throws_WhenTheLocaleIsNotOneTheApplicationStores()
    {
        var provider = IntegrationTestSetup.GetServices();

        var stored = await ReadAsync(provider);

        var incident = await Assert.ThrowsAsync<IncidentException>(async () =>
            await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
            {
                Id = stored.Id,
                VersionLocal = stored.VersionLocal,
                Markdown = { ["xx"] = "# Nowhere" },
            }, CancellationToken.None));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Fact]
    public async Task Handle_Throws_WhenTheCallerHoldsAStaleCopy()
    {
        var provider = IntegrationTestSetup.GetServices();

        var stored = await ReadAsync(provider);

        var incident = await Assert.ThrowsAsync<IncidentException>(async () =>
            await CreatePostHandler(provider).Handle(new ContentPostFeature.Command
            {
                Id = stored.Id,
                VersionLocal = stored.VersionLocal - 1,
                Markdown = { [_english] = "# Stale" },
            }, CancellationToken.None));

        Assert.Equal(IncidentCode.VersionMismatch, incident.Code);
    }
}
