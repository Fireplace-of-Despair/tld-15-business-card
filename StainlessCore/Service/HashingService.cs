// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StainlessCore.Composition;

namespace StainlessCore.Service;

/// <summary> Hashes a plain text with PBKDF2-HMAC-SHA384, a per-value salt and a shared pepper. </summary>
/// <param name="configuration">Configuration that holds the pepper and the salt size.</param>
public sealed class HashingService(IConfiguration configuration)
{
    private const int DefaultIterations = 600_000;
    private const int DefaultHashBytes = 48;

    private readonly string _pepper = configuration.GetSection(Globals.Security.Pepper).Value!;
    private readonly int _saltSize = int.Parse(configuration.GetSection(Globals.Security.SaltSize).Value!);

    /// <summary> Hashing result </summary>
    /// <param name="HexHash">Hash in hex</param>
    /// <param name="HexSalt">Salt in hex</param>
    public record Result(string HexHash, string HexSalt);

    /// <summary> Hash plain text </summary>
    /// <param name="plainText"> Plain text </param>
    /// <param name="hexSalt"> Salt as hex string. If hexSalt is null, the new salt will be created </param>
    /// <returns> <seealso cref="Result"/> </returns>
    public Result Hash(string plainText, string? hexSalt = null)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        byte[]? plainBytes = null;
        byte[]? pepperBytes = null;
        byte[]? textWithPepper = null;
        byte[]? salt = null;
        byte[]? derived = null;

        try
        {
            plainBytes = Encoding.UTF8.GetBytes(plainText);
            pepperBytes = Encoding.UTF8.GetBytes(_pepper);

            textWithPepper = new byte[plainBytes.Length + pepperBytes.Length];
            plainBytes.CopyTo(textWithPepper, 0);
            pepperBytes.CopyTo(textWithPepper, plainBytes.Length);

            salt = new byte[_saltSize];
            if (string.IsNullOrEmpty(hexSalt))
            {
                RandomNumberGenerator.Fill(salt);
            }
            else
            {
                salt = Convert.FromHexString(hexSalt);
            }

            derived = Rfc2898DeriveBytes.Pbkdf2(
                textWithPepper,
                salt,
                DefaultIterations,
                HashAlgorithmName.SHA384,
                DefaultHashBytes);

            var hexHash = Convert.ToHexString(derived);
            var hexSaltOut = Convert.ToHexString(salt);

            return new Result(hexHash, hexSaltOut);
        }
        finally
        {
            if (plainBytes is not null) { CryptographicOperations.ZeroMemory(plainBytes); }
            if (pepperBytes is not null) { CryptographicOperations.ZeroMemory(pepperBytes); }
            if (textWithPepper is not null) { CryptographicOperations.ZeroMemory(textWithPepper); }
            if (salt is not null) { CryptographicOperations.ZeroMemory(salt); }
            if (derived is not null) { CryptographicOperations.ZeroMemory(derived); }
        }
    }

    /// <summary> Compare two hash strings without leaking where they start to differ </summary>
    /// <param name="left">First value</param>
    /// <param name="right">Second value</param>
    /// <returns><c>true</c> when both values match</returns>
    /// <remarks>
    /// The comparison runs over the strings themselves, not over decoded bytes. A stored value is not
    /// always valid hex, so a decode step could throw on it. A length difference alone returns
    /// <c>false</c>.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(
            MemoryMarshal.AsBytes(left.AsSpan()),
            MemoryMarshal.AsBytes(right.AsSpan()));
    }
}
