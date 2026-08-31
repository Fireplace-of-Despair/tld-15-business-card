// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using Microsoft.Extensions.Caching.Memory;
using tld15Server.Composition;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15ServerTests.Application.UnitTests.Services;

[Trait("Application", "Unit Tests")]
public class CacheManager_Tests
{
    private static readonly DateTimeOffset _expiration = DateTimeOffset.UtcNow.AddMinutes(480);

    private static SharedSession CreateSession(Guid accountId, Guid? sessionId = null) => new()
    {
        Id = sessionId ?? Guid.NewGuid(),
        AccountId = accountId,
        Login = "tester",
        Features = ["accounts.get"],
        UserAgent = "xunit",
        UserIP = "127.0.0.1",
        AcceptLanguage = "en",
        AcceptEncoding = "gzip",
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = _expiration
    };

    [Fact]
    public void CleanupExpiredTokens_KeepsSessionsThatAreStillCached()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var session = CreateSession(Guid.NewGuid());
        manager.Save(session, _expiration);

        manager.CleanupExpiredTokens(null);

        Assert.Single(manager.GetAllSessions(), x => x.Id == session.Id);
    }

    [Fact]
    public void CleanupExpiredTokens_DropsSessionsEvictedFromCache()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var session = CreateSession(Guid.NewGuid());
        manager.Save(session, _expiration);

        // Evict behind the manager's back, the way the memory cache does when the entry expires.
        cache.Remove($"{Globals.Cache.SessionPrefix}{session.Id}");

        manager.CleanupExpiredTokens(null);

        Assert.Empty(manager.GetAllSessions());
    }

    [Fact]
    public void RemoveSessionByAccount_AfterCleanupRan_StillRevokesTheSession()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var accountId = Guid.NewGuid();
        var session = CreateSession(accountId);
        manager.Save(session, _expiration);

        // The cleanup timer runs every 30 minutes; a session must survive it to stay revocable.
        manager.CleanupExpiredTokens(null);
        manager.RemoveSessionByAccount(accountId);

        Assert.Null(manager.GetSessionById(session.Id));
        Assert.Empty(manager.GetAllSessions());
    }

    [Fact]
    public void RemoveSessionByAccount_AfterCleanupRan_StillRevokesTheApiKeys()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var accountId = Guid.NewGuid();
        manager.Save("tester-key", accountId, ["stainless_tasks.pull"]);
        manager.Save(CreateSession(accountId), _expiration);

        manager.CleanupExpiredTokens(null);
        manager.RemoveSessionByAccount(accountId);

        Assert.Null(manager.GetAccountIdByApiKey("tester-key"));
        Assert.Empty(manager.GetFeaturesByApiKey("tester-key"));
    }

    [Fact]
    public void Save_SameSessionIdTwice_KeepsTheAccountMapping()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var accountId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        manager.Save(CreateSession(accountId, sessionId), _expiration);
        manager.Save(CreateSession(accountId, sessionId), _expiration);

        manager.RemoveSessionByAccount(accountId);

        Assert.Null(manager.GetSessionById(sessionId));
    }

    [Fact]
    public void RemoveSessionByAccount_LeavesOtherAccountsAlone()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var manager = new CacheManager(cache);

        var doomed = CreateSession(Guid.NewGuid());
        var survivor = CreateSession(Guid.NewGuid());
        manager.Save(doomed, _expiration);
        manager.Save(survivor, _expiration);

        manager.CleanupExpiredTokens(null);
        manager.RemoveSessionByAccount(doomed.AccountId);

        Assert.Null(manager.GetSessionById(doomed.Id));
        Assert.NotNull(manager.GetSessionById(survivor.Id));
    }
}
