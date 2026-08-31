// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Features.Accounts;
using tld15Server.Features.Shared.Identity;

namespace tld15ServerTests.Application.IntegrationTests.Features.Accounts;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class AccountsApiKeys_Tests
{
    private static AccountsPostFeature.Handler CreateHandler(IServiceProvider provider)
    {
        return new AccountsPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<ApiKeyService>()
        );
    }

    private static SharedAccount CreateAccount() => new()
    {
        Login = "api-key-test-" + Guid.NewGuid().ToString("N"),
        Password = "sa",
        StatusId = tld15Server.Composition.Globals.Reference.AccountStatus.Enabled,
        TypeId = tld15Server.Composition.Globals.Reference.AccountType.User,
    };

    // version_local is owned by a database trigger (0 on insert, +1 per update), so the value to send
    // back has to be read rather than assumed.
    private static async Task<long> StoredVersionAsync(IServiceProvider provider, Guid accountId)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        using var context = await factory.CreateDbContextAsync(CancellationToken.None);

        return await context.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => x.VersionLocal)
            .FirstAsync(CancellationToken.None);
    }

    private static async Task<List<StainlessInfrastructure.Models.Identity.ApiKey>> StoredKeysAsync(
        IServiceProvider provider, Guid accountId)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        using var context = await factory.CreateDbContextAsync(CancellationToken.None);

        return await context.ApiKeys.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .ToListAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NewKey_IsReturnedOnce_AndStoredHashed()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);

        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey());

        var result = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        var issued = Assert.Single(result.CreatedApiKeys);
        Assert.False(string.IsNullOrWhiteSpace(issued.Value));

        var stored = Assert.Single(await StoredKeysAsync(provider, result.Id));
        Assert.Equal(issued.Id, stored.Id);

        // What lands in the database must be the hash, never the key that was handed out.
        Assert.NotEqual(issued.Value, stored.KeyHash);
        Assert.Equal(provider.GetRequiredService<ApiKeyService>().Hash(issued.Value!), stored.KeyHash);
    }

    // The UI issues the key when the user adds it, so it can be shown before saving. That exact value
    // has to be the one stored, otherwise the key the user copied would not work.
    [Fact]
    public async Task KeyChosenByTheCaller_IsTheOneStored()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);
        var apiKeyService = provider.GetRequiredService<ApiKeyService>();

        var value = ApiKeyService.Generate();
        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey { Value = value });

        var result = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        var stored = Assert.Single(await StoredKeysAsync(provider, result.Id));
        Assert.Equal(apiKeyService.Hash(value), stored.KeyHash);
        Assert.Equal(value, Assert.Single(result.CreatedApiKeys).Value);
    }

    [Fact]
    public async Task NewKey_StartsUnused()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);

        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey());

        var result = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        var stored = Assert.Single(await StoredKeysAsync(provider, result.Id));
        Assert.Null(stored.LastUsedAt);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    // Saving an account again must not silently re-issue or rotate the keys it already has.
    [Fact]
    public async Task ExistingKey_SurvivesAnotherSave_Unchanged()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);

        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey());

        var created = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);
        var originalHash = (await StoredKeysAsync(provider, created.Id)).Single().KeyHash;

        account.Id = created.Id;
        account.VersionLocal = await StoredVersionAsync(provider, created.Id);
        account.Password = null;
        account.ApiKeys = [new SharedApiKey { Id = created.CreatedApiKeys[0].Id }];

        var resaved = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        Assert.Empty(resaved.CreatedApiKeys);

        var stored = Assert.Single(await StoredKeysAsync(provider, created.Id));
        Assert.Equal(originalHash, stored.KeyHash);
    }

    [Fact]
    public async Task DroppedKey_IsRemoved()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);

        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey());

        var created = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        account.Id = created.Id;
        account.VersionLocal = await StoredVersionAsync(provider, created.Id);
        account.Password = null;
        account.ApiKeys = [];

        await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        Assert.Empty(await StoredKeysAsync(provider, created.Id));
    }

    [Fact]
    public async Task SeveralNewKeys_AreAllIssuedDistinctly()
    {
        var provider = IntegrationTestSetup.GetServices();
        var handler = CreateHandler(provider);

        var account = CreateAccount();
        account.ApiKeys.Add(new SharedApiKey());
        account.ApiKeys.Add(new SharedApiKey());

        var result = await handler.Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        Assert.Equal(2, result.CreatedApiKeys.Count);
        Assert.Equal(2, result.CreatedApiKeys.Select(x => x.Value).Distinct().Count());
        Assert.Equal(2, (await StoredKeysAsync(provider, result.Id)).Select(x => x.KeyHash).Distinct().Count());
    }
}
