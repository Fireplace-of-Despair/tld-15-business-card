// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using StainlessCore.Composition;
using StainlessCore.Exceptions;

namespace StainlessCore.Service;

/// <summary>
/// Counts failed sign-ins and locks out once a threshold is crossed.
/// Counting happens twice: per login and per source address. Per login alone lets an attacker lock
/// somebody else out on purpose, per address alone is defeated by a botnet, so neither is enough by itself.
/// </summary>
public sealed class LoginThrottle(IMemoryCache cache, IConfiguration configuration)
{
    private readonly int _maxAttemptsPerLogin = configuration.GetValue<int>(Globals.Security.LoginMaxAttempts)!;
    private readonly int _maxAttemptsPerAddress = configuration.GetValue<int>(Globals.Security.LoginMaxAttemptsPerAddress)!;
    private readonly int _lockoutMinutes = configuration.GetValue<int>(Globals.Security.LoginLockoutMinutes)!;

    /// <summary> Failed sign-ins allowed per login before it is locked out </summary>
    public int MaxAttemptsPerLogin => _maxAttemptsPerLogin;

    /// <summary> Failed sign-ins allowed per source address before it is locked out </summary>
    public int MaxAttemptsPerAddress => _maxAttemptsPerAddress;

    // The window end travels with the counter: re-Setting an entry would otherwise drop its expiration
    // and a locked-out login would never recover.
    private sealed record Attempts(int Count, DateTimeOffset ExpiresAt);

    /// <summary> Throws <see cref="IncidentCode.TooManyAttempts"/> when the login or the address is locked out </summary>
    /// <param name="login"> Login being attempted </param>
    /// <param name="address"> Source address of the attempt, when known </param>
    public void EnsureNotLockedOut(string? login, string? address)
    {
        if (IsOverLimit(LoginKey(login), _maxAttemptsPerLogin)
         || IsOverLimit(AddressKey(address), _maxAttemptsPerAddress))
        {
            throw new IncidentException(IncidentCode.TooManyAttempts);
        }
    }

    /// <summary> Records a failed sign-in against both the login and the source address </summary>
    public void RegisterFailure(string? login, string? address)
    {
        Increment(LoginKey(login));
        Increment(AddressKey(address));
    }

    /// <summary> Clears the counters after a sign-in succeeds </summary>
    public void RegisterSuccess(string? login, string? address)
    {
        Remove(LoginKey(login));
        Remove(AddressKey(address));
    }

    private bool IsOverLimit(string? key, int limit)
    {
        if (key == null || limit <= 0) { return false; }

        return cache.Get<Attempts>(key)?.Count >= limit;
    }

    private void Increment(string? key)
    {
        if (key == null || _lockoutMinutes <= 0) { return; }

        var now = DateTimeOffset.UtcNow;
        var current = cache.Get<Attempts>(key);

        // The window starts at the first failure and is never extended, so a slow drip of attempts
        // cannot keep a lockout alive forever.
        var expiresAt = current != null && current.ExpiresAt > now
            ? current.ExpiresAt
            : now.AddMinutes(_lockoutMinutes);

        cache.Set(key, new Attempts((current?.Count ?? 0) + 1, expiresAt), expiresAt);
    }

    private void Remove(string? key)
    {
        if (key == null) { return; }

        cache.Remove(key);
    }

    private static string? LoginKey(string? login)
    {
        return string.IsNullOrWhiteSpace(login)
            ? null
            : $"{Globals.Cache.LoginAttemptsByLoginPrefix}{login.ToLowerInvariant()}";
    }

    private static string? AddressKey(string? address)
    {
        return string.IsNullOrWhiteSpace(address)
            ? null
            : $"{Globals.Cache.LoginAttemptsByAddressPrefix}{address}";
    }
}
