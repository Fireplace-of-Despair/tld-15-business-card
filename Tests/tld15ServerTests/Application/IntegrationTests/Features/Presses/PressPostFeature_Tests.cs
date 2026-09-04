// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Exceptions;
using StainlessInfrastructure;
using tld15Server.Features.Presses;
using tld15Server.Features.Shared.Business;

namespace tld15ServerTests.Application.IntegrationTests.Features.Presses;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class PressPostFeature_Tests
{
    private const string _english = "en";
    private const string _japanese = "ja";

    private static PressPostFeature.Handler CreatePostHandler(IServiceProvider provider)
    {
        return new PressPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static PressGetFeature.Handler CreateGetHandler(IServiceProvider provider)
    {
        return new PressGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static Task<PressGetFeature.Result> ReadAsync(IServiceProvider provider, Guid? id)
    {
        return CreateGetHandler(provider)
            .Handle(new PressGetFeature.Query { Id = id }, CancellationToken.None)
            .AsTask();
    }

    private static SharedPress NewMention() => new()
    {
        Url = "https://example.org/they-wrote-about-us",
        PosterUrl = "https://example.org/poster.png",
        PublishedAt = new DateTimeOffset(2021, 3, 9, 0, 0, 0, TimeSpan.Zero),
        Translations =
        [
            new SharedPressTranslation
            {
                LanguageId = _english,
                Title = "Somebody wrote about us",
                Subtitle = "In a paper nobody reads",
                PosterAlt = "A newspaper",
            }
        ]
    };

    private static async Task<Guid> StoreAsync(IServiceProvider provider, SharedPress press, long versionLocal = 0)
    {
        var result = await CreatePostHandler(provider)
            .Handle(new PressPostFeature.Command { Press = press, VersionLocal = versionLocal }, CancellationToken.None);

        return result.Id;
    }

    private static Task DeleteAsync(IServiceProvider provider, Guid id, long versionLocal)
    {
        return new PressDeleteFeature.Handler(provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>())
            .Handle(new PressDeleteFeature.Command { Id = id, VersionLocal = versionLocal }, CancellationToken.None)
            .AsTask();
    }

    [Fact]
    public async Task Handle_CreatesTheMention_AndKeepsTheDateOfThePublication()
    {
        var provider = IntegrationTestSetup.GetServices();

        var id = await StoreAsync(provider, NewMention());

        var result = await ReadAsync(provider, id);

        Assert.Equal("https://example.org/they-wrote-about-us", result.Item.Url);
        Assert.Equal("https://example.org/poster.png", result.Item.PosterUrl);
        Assert.Equal(2021, result.Item.PublishedAt.Year);
        Assert.True(result.CreatedAt.Year > 2021);

        var translation = Assert.Single(result.Item.Translations);
        Assert.Equal("Somebody wrote about us", translation.Title);

        await DeleteAsync(provider, id, result.VersionLocal);
    }

    [Fact]
    public async Task Handle_DropsALocaleThatCarriesNothing()
    {
        var provider = IntegrationTestSetup.GetServices();

        var mention = NewMention();
        mention.Translations.Add(new SharedPressTranslation { LanguageId = _japanese });

        var id = await StoreAsync(provider, mention);

        var result = await ReadAsync(provider, id);

        Assert.Equal(_english, Assert.Single(result.Item.Translations).LanguageId);

        await DeleteAsync(provider, id, result.VersionLocal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/somewhere/on/this/site")]
    public async Task Handle_Throws_WhenTheMentionLeadsNowhereAReaderCanFollow(string url)
    {
        var provider = IntegrationTestSetup.GetServices();

        var mention = NewMention();
        mention.Url = url;

        var incident = await Assert.ThrowsAsync<IncidentException>(async () => await StoreAsync(provider, mention));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    public async Task Handle_Throws_WhenThePosterIsNotAnAddressAPictureLoadsFrom(string posterUrl)
    {
        var provider = IntegrationTestSetup.GetServices();

        var mention = NewMention();
        mention.PosterUrl = posterUrl;

        var incident = await Assert.ThrowsAsync<IncidentException>(async () => await StoreAsync(provider, mention));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Fact]
    public async Task Handle_Throws_WhenTheCallerHoldsAStaleCopy()
    {
        var provider = IntegrationTestSetup.GetServices();

        var id = await StoreAsync(provider, NewMention());
        var stored = await ReadAsync(provider, id);

        var incident = await Assert.ThrowsAsync<IncidentException>(async () =>
            await StoreAsync(provider, stored.Item, stored.VersionLocal - 1));

        Assert.Equal(IncidentCode.VersionMismatch, incident.Code);

        await DeleteAsync(provider, id, stored.VersionLocal);
    }

    [Fact]
    public async Task Handle_OpensAnEmptyMention_WhenTheQueryNamesNoId()
    {
        var provider = IntegrationTestSetup.GetServices();

        var result = await ReadAsync(provider, null);

        Assert.Null(result.Item.Id);
        Assert.Empty(result.Item.Url);
        Assert.NotEmpty(result.Languages);
    }

    [Fact]
    public async Task List_ReturnsTheMentionAsACardThatLeadsOffTheSite()
    {
        var provider = IntegrationTestSetup.GetServices();

        var id = await StoreAsync(provider, NewMention());

        var listed = await new PressListFeature.Handler(provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>())
            .Handle(new PressListFeature.Query { Language = _english }, CancellationToken.None);

        var card = Assert.Single(listed.Items, x => x.Id == id.ToString());

        Assert.Equal("https://example.org/they-wrote-about-us", card.ExternalUrl);
        Assert.Equal("Somebody wrote about us", card.Title);
        // A mention belongs to nobody here, so the card draws no division badge and no link buttons.
        Assert.Empty(card.DivisionId);
        Assert.Empty(card.ProjectTypeId);

        var stored = await ReadAsync(provider, id);
        await DeleteAsync(provider, id, stored.VersionLocal);
    }
}
