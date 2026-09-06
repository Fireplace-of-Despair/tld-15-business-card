// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StainlessCore.Composition;

namespace StainlessCore.Service;

/// <summary>
/// Issues API keys and turns them into the hash that is stored and looked up.
/// </summary>
/// <remarks>
/// Deliberately a plain SHA-512 over key+pepper rather than the PBKDF2 used for passwords. A key is
/// 256 bits of CSPRNG output, so there is nothing to brute-force, and every protected request hashes
/// the presented key to find its account - a per-key salt would force a full table scan, and a slow
/// KDF would put ~600k iterations on the hot path.
/// </remarks>
public sealed class ApiKeyService(IConfiguration configuration)
{
    private const int KeyBytes = 32;

    private readonly string _pepper = configuration.GetSection(Globals.Security.Pepper).Value ?? string.Empty;

    /// <summary> Creates a new key. This is the only moment its plain text exists. </summary>
    public static string Generate()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(KeyBytes));
    }

    /// <summary> Hashes a key into the form stored in the database and used as the cache key. </summary>
    public string Hash(string apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey);

        byte[]? keyWithPepper = null;

        try
        {
            keyWithPepper = Encoding.UTF8.GetBytes(apiKey + _pepper);

            return Convert.ToHexString(SHA512.HashData(keyWithPepper));
        }
        finally
        {
            if (keyWithPepper is not null) { CryptographicOperations.ZeroMemory(keyWithPepper); }
        }
    }

    // Base64url keeps the key safe to paste into a header or a URL without escaping.
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
