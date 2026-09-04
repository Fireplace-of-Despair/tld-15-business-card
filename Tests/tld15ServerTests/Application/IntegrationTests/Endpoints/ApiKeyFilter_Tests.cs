// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Endpoints;
using tld15Server.Features.Accounts;
using tld15Server.Features.Shared.Identity;
using tld15Server.Services;


namespace tld15ServerTests.Application.IntegrationTests.Endpoints;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class ApiKeyFilter_Tests
{
    private static async Task<(Guid AccountId, string Key)> IssueKeyAsync(IServiceProvider provider)
    {
        var handler = new AccountsPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<ApiKeyService>()
        );

        var account = new SharedAccount
        {
            Login = "filter-test-" + Guid.NewGuid().ToString("N"),
            Password = "sa",
            StatusId = tld15Server.Composition.Globals.Reference.AccountStatus.Enabled,
            TypeId = tld15Server.Composition.Globals.Reference.AccountType.User,
        };
        account.ApiKeys.Add(new SharedApiKey());

        var result = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        return (result.Id, result.CreatedApiKeys[0].Value!);
    }

    private static ApiKeyFilter CreateFilter(IServiceProvider provider)
    {
        return new ApiKeyFilter
        (
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<ApiKeyService>(),
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>()
        );
    }

    private static async Task<DateTimeOffset?> LastUsedAtAsync(IServiceProvider provider, Guid accountId)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        using var context = await factory.CreateDbContextAsync(CancellationToken.None);

        return await context.ApiKeys.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => x.LastUsedAt)
            .FirstAsync(CancellationToken.None);
    }

    [Fact]
    public async Task UsingAKey_StampsLastUsedAt()
    {
        var provider = IntegrationTestSetup.GetServices();
        var (accountId, key) = await IssueKeyAsync(provider);

        Assert.Null(await LastUsedAtAsync(provider, accountId));

        var before = DateTimeOffset.UtcNow;
        await CreateFilter(provider).TouchLastUsedAsync(
            provider.GetRequiredService<ApiKeyService>().Hash(key), CancellationToken.None);

        var lastUsedAt = await LastUsedAtAsync(provider, accountId);

        Assert.NotNull(lastUsedAt);
        Assert.InRange(lastUsedAt.Value, before.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task UsingAKeyAgain_MovesLastUsedAtForward()
    {
        var provider = IntegrationTestSetup.GetServices();
        var (accountId, key) = await IssueKeyAsync(provider);
        var filter = CreateFilter(provider);
        var hash = provider.GetRequiredService<ApiKeyService>().Hash(key);

        await filter.TouchLastUsedAsync(hash, CancellationToken.None);
        var first = await LastUsedAtAsync(provider, accountId);

        await Task.Delay(50, CancellationToken.None);

        await filter.TouchLastUsedAsync(hash, CancellationToken.None);
        var second = await LastUsedAtAsync(provider, accountId);

        Assert.True(second > first, $"expected {second:O} to be later than {first:O}");
    }

    // A hash that belongs to nobody must not touch anyone else's row.
    [Fact]
    public async Task UnknownKey_LeavesEveryRowAlone()
    {
        var provider = IntegrationTestSetup.GetServices();
        var (accountId, _) = await IssueKeyAsync(provider);

        await CreateFilter(provider).TouchLastUsedAsync(
            provider.GetRequiredService<ApiKeyService>().Hash(ApiKeyService.Generate()), CancellationToken.None);

        Assert.Null(await LastUsedAtAsync(provider, accountId));
    }
}
