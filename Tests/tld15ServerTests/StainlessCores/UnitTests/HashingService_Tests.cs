// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using StainlessCore.Service;

namespace tld15ServerTests.StainlessCores.UnitTests;

[Trait("Category", "StainlessCore")]
public class HashingService_Tests
{
    private static IConfiguration CreateConfiguration()
    {
        var appSettings = @"{""Security"":{
            ""Pepper"" : ""1234567890"",
            ""SaltSize"" : ""128""
            }}";

        var builder = new ConfigurationBuilder();

        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appSettings)));

        return builder.Build();
    }

    private readonly HashingService _hashingService = new(CreateConfiguration());

    [Theory]
    [InlineData("sa")]
    [InlineData("12")]
    [InlineData("test")]
    [InlineData("password")]
    public void Hash_MustHashString(string plainText)
    {
        var result = _hashingService.Hash(plainText);

        Assert.NotNull(result);
        Assert.NotNull(result.HexHash);
        Assert.NotNull(result.HexSalt);
    }


    [Theory]
    [InlineData(null)]
    public void Hash_MustThrowArgumentExceptionForNull(string? plainText)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        Assert.Throws<ArgumentNullException>(() => _hashingService.Hash(plainText));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("12")]
    [InlineData("test")]
    [InlineData("password")]
    public void Hash_MustBeDifferentForSameText(string plainText)
    {
        var resultOne = _hashingService.Hash(plainText);
        var resultTwo = _hashingService.Hash(plainText);

        Assert.NotEqual(resultOne.HexHash, resultTwo.HexHash);
        Assert.NotEqual(resultOne.HexSalt, resultTwo.HexSalt);
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("12")]
    [InlineData("test")]
    [InlineData("password")]
    public void Hash_Restore(string plaintText)
    {
        var initialHashing = _hashingService.Hash(plaintText);

        var sameWithSalt = _hashingService.Hash(plaintText, initialHashing.HexSalt);

        Assert.Equal(initialHashing.HexHash, sameWithSalt.HexHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A1B2")]
    [InlineData("0x0")]
    public void FixedTimeEquals_MatchesAnIdenticalValue(string value)
    {
        Assert.True(_hashingService.FixedTimeEquals(value, value));
    }

    [Theory]
    [InlineData("A1B2", "A1B3")]
    [InlineData("A1B2", "B1B2")]
    [InlineData("A1B2", "a1b2")]
    public void FixedTimeEquals_RejectsADifferentValue(string left, string right)
    {
        Assert.False(_hashingService.FixedTimeEquals(left, right));
    }

    // A stored value is not always valid hex, so the comparison must not decode it.
    [Theory]
    [InlineData("A1B2", "A1B")]
    [InlineData("A1B2", "A1B23")]
    [InlineData("A1B2", "")]
    public void FixedTimeEquals_RejectsADifferentLength(string left, string right)
    {
        Assert.False(_hashingService.FixedTimeEquals(left, right));
    }
}
