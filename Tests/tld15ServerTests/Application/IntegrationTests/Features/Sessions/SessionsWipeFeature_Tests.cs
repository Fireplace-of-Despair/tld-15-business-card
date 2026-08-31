// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Features.Accounts;
using tld15Server.Features.Sessions;
using tld15Server.Features.Shared.Identity;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15ServerTests.Application.IntegrationTests.Features.Sessions;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class SessionsWipeFeature_Tests
{
    private const string _feature = "profile.get";

    private static readonly DateTimeOffset _expiration = DateTimeOffset.UtcNow.AddMinutes(480);

    private static SessionsWipeFeature.Handler CreateHandler(IServiceProvider provider)
    {
        return new SessionsWipeFeature.Handler
        (
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>()
        );
    }

    private static AccountsPostFeature.Handler CreateAccountHandler(IServiceProvider provider)
    {
        return new AccountsPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<ApiKeyService>()
        );
    }

    private static async Task<(Guid AccountId, string KeyHash)> CreateAccountWithKeyAsync(IServiceProvider provider)
    {
        var account = new SharedAccount
        {
            Login = "wipe-test-" + Guid.NewGuid().ToString("N"),
            Password = "sa",
            StatusId = tld15Server.Composition.Globals.Reference.AccountStatus.Enabled,
            TypeId = tld15Server.Composition.Globals.Reference.AccountType.User,
            Features = [_feature],
        };
        account.ApiKeys.Add(new SharedApiKey());

        var created = await CreateAccountHandler(provider)
            .Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        var value = Assert.Single(created.CreatedApiKeys).Value!;

        return (created.Id, provider.GetRequiredService<ApiKeyService>().Hash(value));
    }

    private static SharedSession CreateSession(Guid accountId) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        Login = "tester",
        Features = [_feature],
        UserAgent = "xunit",
        UserIP = "127.0.0.1",
        AcceptLanguage = "en",
        AcceptEncoding = "gzip",
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = _expiration
    };

    [Fact]
    public async Task Wipe_DropsTheSessionsOfTheAccount()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var cache = provider.GetRequiredService<CacheManager>();
        var (accountId, _) = await CreateAccountWithKeyAsync(provider);

        var session = CreateSession(accountId);
        cache.Save(session, _expiration);

        await CreateHandler(provider).Handle(
            new SessionsWipeFeature.Command { AccountId = accountId }, CancellationToken.None);

        Assert.Null(cache.GetSessionById(session.Id));
    }

    // The wipe takes the API keys down together with the sessions, because both hang on the account.
    // A key stays valid until somebody deletes it, so the cache must hold it again once the wipe ends.
    [Fact]
    public async Task Wipe_PutsTheApiKeysBackIntoTheCache()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var cache = provider.GetRequiredService<CacheManager>();
        var (accountId, keyHash) = await CreateAccountWithKeyAsync(provider);

        await CreateHandler(provider).Handle(
            new SessionsWipeFeature.Command { AccountId = accountId }, CancellationToken.None);

        Assert.Equal(accountId, cache.GetAccountIdByApiKey(keyHash));
        Assert.Contains(_feature, cache.GetFeaturesByApiKey(keyHash));
    }

}
