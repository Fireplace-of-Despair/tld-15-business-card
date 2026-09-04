// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using tld15Server.Composition;
using tld15Server.Features.Shared.System;

namespace tld15Server.Services;

//TODO: A bit of a mess, point for future refactoring
public class CacheManager : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<Guid, Guid> _sessionToUser = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeyToUser = new();
    private readonly Timer _cleanupTimer;

    public CacheManager(IMemoryCache cache)
    {
        _cleanupTimer = new Timer(CleanupExpiredTokens, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        _cache = cache;
    }

    // Cache keys are built here and nowhere else: a session lookup that misses the prefix silently
    // reports every live session as expired, which is how the cleanup timer used to wipe the map.
    private static string SessionKey(Guid sessionId) => $"{Globals.Cache.SessionPrefix}{sessionId}";
    private static string ApiKeyKey(string apiKeyHash) => $"{Globals.Cache.ApiKeyPrefix}{apiKeyHash}";

    // Keys are tracked by their hash, never by the key itself - the plain text only ever exists in the
    // response that issued it.
    public void Save(string apiKeyHash, Guid accountId, List<string> features)
    {
        _apiKeyToUser.AddOrUpdate(apiKeyHash, accountId, (_, _) => accountId);
        _cache.Set(ApiKeyKey(apiKeyHash), features);
    }

    public Guid? GetAccountIdByApiKey(string? apiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(apiKeyHash)) { return null; }

        return _apiKeyToUser.TryGetValue(apiKeyHash, out var accountId) ? accountId : null;
    }

    public List<string> GetFeaturesByApiKey(string? apiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(apiKeyHash)) { return []; }

        var features = _cache.Get<List<string>>(ApiKeyKey(apiKeyHash));
        if (features == null || features.Count == 0)
        {
            _apiKeyToUser.TryRemove(apiKeyHash, out _);
        }
        return features ?? [];
    }

    public void Save(SharedSession session, DateTimeOffset expiration)
    {
        _sessionToUser.AddOrUpdate(session.Id, session.AccountId, (_, _) => session.AccountId);
        _cache.Set(SessionKey(session.Id), session, expiration);
    }

    public SharedSession? GetSessionById(Guid sessionId)
    {
        var session = _cache.Get<SharedSession>(SessionKey(sessionId));
        if (session == null)
        {
            _sessionToUser.TryRemove(sessionId, out _);
        }

        return session;
    }

    public IEnumerable<SharedSession> GetAllSessions()
    {
        var result = new List<SharedSession>();

        foreach (var item in _sessionToUser)
        {
            var session = _cache.Get<SharedSession>(SessionKey(item.Key));
            if (session != null)
            {
                result.Add(session);
            }
            else
            {
                _sessionToUser.TryRemove(item.Key, out _);
            }
        }
        return result;
    }

    public void RemoveSessionByAccount(Guid accountId)
    {
        var userSessions = _sessionToUser.Where(x => x.Value == accountId).Select(x => x.Key).ToList();
        foreach (var item in userSessions)
        {
            _sessionToUser.TryRemove(item, out _);
            _cache.Remove(SessionKey(item));
        }

        var userApiKeys = _apiKeyToUser.Where(x => x.Value == accountId).Select(x => x.Key).ToList();
        foreach (var item in userApiKeys)
        {
            _apiKeyToUser.TryRemove(item, out _);
            _cache.Remove(ApiKeyKey(item));
        }
    }

    public void RemoveSessionById(Guid sessionId)
    {
        _sessionToUser.TryRemove(sessionId, out _);
        _cache.Remove(SessionKey(sessionId));
    }

    internal void CleanupExpiredTokens(object? state)
    {
        foreach (var token in _sessionToUser.Keys.ToList())
        {
            if (!_cache.TryGetValue<SharedSession>(SessionKey(token), out _))
            {
                _sessionToUser.TryRemove(token, out _);
            }
        }
    }

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cleanupTimer?.Dispose();
        }
    }

    #endregion
}
