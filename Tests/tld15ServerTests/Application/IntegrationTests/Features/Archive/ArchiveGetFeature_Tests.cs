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
using tld15Server.Features.Archive;
using tld15Server.Features.Home;
using tld15Server.Features.Projects;
using tld15Server.Features.Shared.Business;

namespace tld15ServerTests.Application.IntegrationTests.Features.Archive;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class ArchiveGetFeature_Tests
{
    private const string _english = "en";

    /// <summary> A division the front page shows. </summary>
    private const string _current = "TLD";

    /// <summary> The names those two divisions carry, as the reference tables import them. </summary>
    private const string _archiveName = "Ashen Chronicles Division";
    private const string _currentName = "Tamed Logic Division";

    private static ProjectPostFeature.Handler CreatePostHandler(IServiceProvider provider)
    {
        return new ProjectPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        );
    }

    private static Task<ArchiveGetFeature.Result> ReadArchiveAsync(IServiceProvider provider)
    {
        return new ArchiveGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        )
            .Handle(new ArchiveGetFeature.Query { Language = _english }, CancellationToken.None)
            .AsTask();
    }

    private static Task<HomeGetFeature.Result> ReadHomeAsync(IServiceProvider provider)
    {
        return new HomeGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextBusiness>>(),
            provider.GetRequiredService<IDbContextFactory<DataContextReference>>()
        )
            .Handle(new HomeGetFeature.Query { Language = _english }, CancellationToken.None)
            .AsTask();
    }

    private static async Task<string> StoreAsync(IServiceProvider provider, string divisionId, string typeId)
    {
        var id = "test-" + Guid.NewGuid().ToString("N");

        await CreatePostHandler(provider).Handle(new ProjectPostFeature.Command
        {
            VersionLocal = 0,
            Project = new SharedProject
            {
                Id = id,
                ProjectTypeId = typeId,
                DivisionId = divisionId,
                PublishedAt = DateTimeOffset.UtcNow,
                Translations = [new SharedProjectTranslation { LanguageId = _english, Title = id }],
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
    public async Task Handle_ReturnsTheWorksOfTheArchiveDivision_AndNothingElse()
    {
        var provider = IntegrationTestSetup.GetServices();

        var archived = await StoreAsync(provider, Globals.Divisions.ACD, Globals.ProjectType.Project);
        var current = await StoreAsync(provider, _current, Globals.ProjectType.Project);

        var result = await ReadArchiveAsync(provider);

        Assert.Contains(result.Projects, x => x.Id == archived);
        Assert.DoesNotContain(result.Projects, x => x.Id == current);
        Assert.All(result.Projects, x => Assert.Equal(Globals.Divisions.ACD, x.DivisionId));
        Assert.All(result.Articles, x => Assert.Equal(Globals.Divisions.ACD, x.DivisionId));

        await DeleteAsync(provider, archived, current);
    }

    [Fact]
    public async Task Handle_SplitsTheWorksTheSameWayTheFrontPageDoes()
    {
        var provider = IntegrationTestSetup.GetServices();

        var article = await StoreAsync(provider, Globals.Divisions.ACD, Globals.ProjectType.Article);
        var project = await StoreAsync(provider, Globals.Divisions.ACD, Globals.ProjectType.Project);

        var result = await ReadArchiveAsync(provider);

        Assert.Contains(result.Articles, x => x.Id == article);
        Assert.Contains(result.Projects, x => x.Id == project);

        await DeleteAsync(provider, article, project);
    }

    [Fact]
    public async Task Handle_NamesTheDivisionOfACard_OnBothWalls()
    {
        var provider = IntegrationTestSetup.GetServices();

        var archived = await StoreAsync(provider, Globals.Divisions.ACD, Globals.ProjectType.Project);
        var current = await StoreAsync(provider, _current, Globals.ProjectType.Project);

        var fromArchive = (await ReadArchiveAsync(provider)).Projects.Single(x => x.Id == archived);
        var fromHome = (await ReadHomeAsync(provider)).Projects.Single(x => x.Id == current);

        // The name of a division comes from the reference tables, which a wall reads separately from
        // the works it shows. A card that echoes its own division id back is a card whose name that
        // read never found.
        Assert.Equal(_archiveName, fromArchive.DivisionName);
        Assert.Equal(_currentName, fromHome.DivisionName);

        await DeleteAsync(provider, archived, current);
    }

    [Fact]
    public async Task HomeGetFeature_LeavesTheArchivedWorksOut()
    {
        var provider = IntegrationTestSetup.GetServices();

        var archived = await StoreAsync(provider, Globals.Divisions.ACD, Globals.ProjectType.Project);
        var current = await StoreAsync(provider, _current, Globals.ProjectType.Project);

        var result = await ReadHomeAsync(provider);

        Assert.Contains(result.Projects, x => x.Id == current);
        Assert.DoesNotContain(result.Projects, x => x.Id == archived);
        Assert.All(result.Projects, x => Assert.NotEqual(Globals.Divisions.ACD, x.DivisionId));
        Assert.All(result.Articles, x => Assert.NotEqual(Globals.Divisions.ACD, x.DivisionId));

        await DeleteAsync(provider, archived, current);
    }
}
