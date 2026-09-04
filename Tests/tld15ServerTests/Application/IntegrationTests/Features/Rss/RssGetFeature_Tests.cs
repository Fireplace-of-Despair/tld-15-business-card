// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Projects;
using tld15Server.Features.Rss;
using tld15Server.Features.Shared.Business;

namespace tld15ServerTests.Application.IntegrationTests.Features.Rss;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class RssGetFeature_Tests
{
    private const string _english = "en";
    private const string _japanese = "ja";
    private const string _division = "TLD";

    private static ProjectPostFeature.Handler CreatePostHandler(IServiceProvider provider)
    {
        return new ProjectPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static Task<RssGetFeature.Result> ReadFeedAsync(IServiceProvider provider, string language)
    {
        return new RssGetFeature.Handler(provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>())
            .Handle(new RssGetFeature.Query { Language = language }, CancellationToken.None)
            .AsTask();
    }

    private static async Task<string> StoreAsync(
          IServiceProvider provider
        , DateTimeOffset publishedAt
        , params SharedProjectTranslation[] translations)
    {
        var id = "test-" + Guid.NewGuid().ToString("N");

        await CreatePostHandler(provider).Handle(new ProjectPostFeature.Command
        {
            VersionLocal = 0,
            Project = new SharedProject
            {
                Id = id,
                ProjectTypeId = Globals.ProjectType.Article,
                DivisionId = _division,
                PublishedAt = publishedAt,
                Translations = [.. translations],
            }
        }, CancellationToken.None);

        return id;
    }

    private static async Task DeleteAsync(IServiceProvider provider, params string[] ids)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>();

        await using var context = await factory.CreateDbContextAsync(CancellationToken.None);
        await context.Projects.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_CarriesTheTitleAndTheSubtitleOfTheRequestedLocale()
    {
        var provider = IntegrationTestSetup.GetServices();

        var id = await StoreAsync(provider, DateTimeOffset.UtcNow,
            new SharedProjectTranslation { LanguageId = _english, Title = "English title", Subtitle = "English line" },
            new SharedProjectTranslation { LanguageId = _japanese, Title = "日本語の題", Subtitle = "日本語の行" });

        var english = await ReadFeedAsync(provider, _english);
        var japanese = await ReadFeedAsync(provider, _japanese);

        var fromEnglish = english.Entries.Single(x => x.Id == id);
        var fromJapanese = japanese.Entries.Single(x => x.Id == id);

        Assert.Equal("English title", fromEnglish.Title);
        Assert.Equal("English line", fromEnglish.Subtitle);
        Assert.Equal("日本語の題", fromJapanese.Title);
        Assert.Equal("日本語の行", fromJapanese.Subtitle);

        await DeleteAsync(provider, id);
    }

    [Fact]
    public async Task Handle_FallsBackToTheLocaleTheWorkCarries()
    {
        var provider = IntegrationTestSetup.GetServices();

        // Only English. A reader who asks in Japanese still hears about the work, in the words it
        // was written in - the same order ProjectReadPage picks a translation in.
        var id = await StoreAsync(provider, DateTimeOffset.UtcNow,
            new SharedProjectTranslation { LanguageId = _english, Title = "Only English", Subtitle = "One line" });

        var japanese = await ReadFeedAsync(provider, _japanese);

        var entry = japanese.Entries.Single(x => x.Id == id);

        Assert.Equal("Only English", entry.Title);

        await DeleteAsync(provider, id);
    }

    [Fact]
    public async Task Handle_LeavesOutAWorkNobodyHasWrittenAWordOf()
    {
        var provider = IntegrationTestSetup.GetServices();

        // A work with no translation has no page to point at: ProjectReadPage answers 404 for one,
        // and a feed that announces a 404 spends a reader's click on nothing.
        var written = await StoreAsync(provider, DateTimeOffset.UtcNow,
            new SharedProjectTranslation { LanguageId = _english, Title = "Written", Subtitle = "Has words" });

        var silent = await StoreAsync(provider, DateTimeOffset.UtcNow);

        var feed = await ReadFeedAsync(provider, _english);

        Assert.Contains(feed.Entries, x => x.Id == written);
        Assert.DoesNotContain(feed.Entries, x => x.Id == silent);

        await DeleteAsync(provider, written, silent);
    }

    [Fact]
    public async Task Handle_OrdersTheEntriesNewestFirst()
    {
        var provider = IntegrationTestSetup.GetServices();

        var now = DateTimeOffset.UtcNow;

        var older = await StoreAsync(provider, now.AddDays(-2),
            new SharedProjectTranslation { LanguageId = _english, Title = "Older", Subtitle = "-" });

        var newer = await StoreAsync(provider, now.AddDays(-1),
            new SharedProjectTranslation { LanguageId = _english, Title = "Newer", Subtitle = "-" });

        var feed = await ReadFeedAsync(provider, _english);

        var indexOfNewer = feed.Entries.FindIndex(x => x.Id == newer);
        var indexOfOlder = feed.Entries.FindIndex(x => x.Id == older);

        Assert.True(indexOfNewer >= 0 && indexOfOlder >= 0, "both works reached the feed");
        Assert.True(indexOfNewer < indexOfOlder, "the newer work stands before the older one");

        await DeleteAsync(provider, older, newer);
    }
}
