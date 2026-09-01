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
using tld15Server.Features.Projects;
using tld15Server.Features.Shared.Business;

namespace tld15ServerTests.Application.IntegrationTests.Features.Projects;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class ProjectPostFeature_Tests
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

    private static ProjectGetFeature.Handler CreateGetHandler(IServiceProvider provider)
    {
        return new ProjectGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static ProjectDeleteFeature.Handler CreateDeleteHandler(IServiceProvider provider)
    {
        return new ProjectDeleteFeature.Handler(provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>());
    }

    private static Task<ProjectGetFeature.Result> ReadAsync(IServiceProvider provider, string? id)
    {
        return CreateGetHandler(provider)
            .Handle(new ProjectGetFeature.Query { Id = id, LanguageId = _english }, CancellationToken.None)
            .AsTask();
    }

    /// <summary> A fresh id per test: the integration tests share one database. </summary>
    private static string NewId() => "test-" + Guid.NewGuid().ToString("N");

    private static SharedProject NewProject(string id) => new()
    {
        Id = id,
        ProjectTypeId = Globals.ProjectType.Article,
        DivisionId = _division,
        PosterUrl = "https://example.org/poster.png",
        PublishedAt = new DateTimeOffset(2019, 5, 4, 0, 0, 0, TimeSpan.Zero),
        Translations =
        [
            new SharedProjectTranslation
            {
                LanguageId = _english,
                Title = "A carried over article",
                Subtitle = "From the old site",
                PosterAlt = "A poster",
                Markdown = "# Body",
            }
        ]
    };

    private static Task StoreAsync(IServiceProvider provider, SharedProject project, long versionLocal = 0)
    {
        return CreatePostHandler(provider)
            .Handle(new ProjectPostFeature.Command { Project = project, VersionLocal = versionLocal }, CancellationToken.None)
            .AsTask();
    }

    [Fact]
    public async Task Handle_CreatesTheWork_AndKeepsTheDateTheEditorSet()
    {
        var provider = IntegrationTestSetup.GetServices();
        var id = NewId();

        var project = NewProject(id);
        project.Links.Add(new SharedLink { Icon = "github", Language = "en", Url = "https://example.org" });

        await StoreAsync(provider, project);

        var result = await ReadAsync(provider, id);

        Assert.Equal(id, result.Item.Id);
        Assert.Equal(Globals.ProjectType.Article, result.Item.ProjectTypeId);
        Assert.Equal(_division, result.Item.DivisionId);
        Assert.Equal("https://example.org/poster.png", result.Item.PosterUrl);

        // The trigger owns created_at, so the date of the work has to be a column of its own.
        Assert.Equal(2019, result.Item.PublishedAt.Year);
        Assert.True(result.CreatedAt.Year > 2019);

        var translation = Assert.Single(result.Item.Translations);
        Assert.Equal("A carried over article", translation.Title);
        Assert.Equal("# Body", translation.Markdown);

        var link = Assert.Single(result.Item.Links);
        Assert.Equal("github", link.Icon);
        Assert.Equal("en", link.Language);

        await CreateDeleteHandler(provider).Handle(
            new ProjectDeleteFeature.Command { Id = id, VersionLocal = result.VersionLocal }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DropsALocaleThatCarriesNothing()
    {
        var provider = IntegrationTestSetup.GetServices();
        var id = NewId();

        var project = NewProject(id);
        project.Translations.Add(new SharedProjectTranslation { LanguageId = _japanese });

        await StoreAsync(provider, project);

        var result = await ReadAsync(provider, id);

        Assert.Equal(_english, Assert.Single(result.Item.Translations).LanguageId);

        await CreateDeleteHandler(provider).Handle(
            new ProjectDeleteFeature.Command { Id = id, VersionLocal = result.VersionLocal }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_RemovesALocaleTheEditorEmptied()
    {
        var provider = IntegrationTestSetup.GetServices();
        var id = NewId();

        var project = NewProject(id);
        project.Translations.Add(new SharedProjectTranslation
        {
            LanguageId = _japanese,
            Title = "運ばれた記事",
        });

        await StoreAsync(provider, project);

        var stored = await ReadAsync(provider, id);
        Assert.Equal(2, stored.Item.Translations.Count);

        stored.Item.Translations.RemoveAll(x => x.LanguageId == _japanese);
        await StoreAsync(provider, stored.Item, stored.VersionLocal);

        var result = await ReadAsync(provider, id);

        Assert.Equal(_english, Assert.Single(result.Item.Translations).LanguageId);

        await CreateDeleteHandler(provider).Handle(
            new ProjectDeleteFeature.Command { Id = id, VersionLocal = result.VersionLocal }, CancellationToken.None);
    }

    [Theory]
    [InlineData("Upper Case")]
    [InlineData("with spaces")]
    [InlineData("слишком-по-русски")]
    [InlineData("")]
    public async Task Handle_Throws_WhenTheIdIsNotOfTheShapeAnAddressCarries(string id)
    {
        var provider = IntegrationTestSetup.GetServices();

        var incident = await Assert.ThrowsAsync<IncidentException>(async () =>
            await StoreAsync(provider, NewProject(id)));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("mailto:someone@example.org")]
    public async Task Handle_Throws_WhenThePosterIsNotAnAddressAPictureLoadsFrom(string posterUrl)
    {
        var provider = IntegrationTestSetup.GetServices();

        var project = NewProject(NewId());
        project.PosterUrl = posterUrl;

        var incident = await Assert.ThrowsAsync<IncidentException>(async () => await StoreAsync(provider, project));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Fact]
    public async Task Handle_Throws_WhenTheDivisionIsNotOneTheApplicationStores()
    {
        var provider = IntegrationTestSetup.GetServices();

        var project = NewProject(NewId());
        project.DivisionId = "XXX";

        var incident = await Assert.ThrowsAsync<IncidentException>(async () => await StoreAsync(provider, project));

        Assert.Equal(IncidentCode.Validation, incident.Code);
    }

    [Fact]
    public async Task Handle_Throws_WhenTheCallerHoldsAStaleCopy()
    {
        var provider = IntegrationTestSetup.GetServices();
        var id = NewId();

        await StoreAsync(provider, NewProject(id));

        var stored = await ReadAsync(provider, id);

        var incident = await Assert.ThrowsAsync<IncidentException>(async () =>
            await StoreAsync(provider, stored.Item, stored.VersionLocal - 1));

        Assert.Equal(IncidentCode.VersionMismatch, incident.Code);

        await CreateDeleteHandler(provider).Handle(
            new ProjectDeleteFeature.Command { Id = id, VersionLocal = stored.VersionLocal }, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_RemovesTheWorkAndTheTranslationsThatHangOffIt()
    {
        var provider = IntegrationTestSetup.GetServices();
        var id = NewId();

        await StoreAsync(provider, NewProject(id));

        var stored = await ReadAsync(provider, id);

        await CreateDeleteHandler(provider).Handle(
            new ProjectDeleteFeature.Command { Id = id, VersionLocal = stored.VersionLocal }, CancellationToken.None);

        var incident = await Assert.ThrowsAsync<IncidentException>(async () => await ReadAsync(provider, id));

        Assert.Equal(IncidentCode.NotFound, incident.Code);
    }

    [Fact]
    public async Task Handle_OpensAnEmptyWork_WhenTheQueryNamesNoId()
    {
        var provider = IntegrationTestSetup.GetServices();

        var result = await ReadAsync(provider, null);

        Assert.Empty(result.Item.Id);
        Assert.NotEmpty(result.Item.ProjectTypeId);
        Assert.NotEmpty(result.Item.DivisionId);
        Assert.NotEmpty(result.Languages);
        Assert.NotEmpty(result.Divisions);
        Assert.NotEmpty(result.ProjectTypes);
    }
}
