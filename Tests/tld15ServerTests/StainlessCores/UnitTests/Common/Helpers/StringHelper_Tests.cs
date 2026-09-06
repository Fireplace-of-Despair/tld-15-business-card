// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using StainlessCore.Common.Helpers;

namespace tld15ServerTests.StainlessCores.UnitTests.Common.Helpers;

[Trait("Category", "StainlessCore")]
public class StringHelper_Tests
{
    #region ToGuid

    [Fact]
    public void ToGuid_ReturnsGuid_WhenStringIsValidGuid()
    {
        var guid = Guid.NewGuid();
        var text = guid.ToString();

        var result = text.ToGuid();

        Assert.NotNull(result);
        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToGuid_ReturnsNull_WhenStringIsInvalidGuid()
    {
        string text = "not-a-guid";

        var result = text.ToGuid();

        Assert.Null(result);
    }

    [Fact]
    public void ToGuid_ReturnsNull_WhenStringIsNull()
    {
        string? text = null;

        var result = text.ToGuid();

        Assert.Null(result);
    }

    [Fact]
    public void ToGuid_ReturnsNull_WhenStringIsEmpty()
    {
        string text = string.Empty;

        var result = text.ToGuid();

        Assert.Null(result);
    }

    #endregion

    #region ToInt

    [Fact]
    public void ToInt_ReturnsInt_WhenStringIsValidInteger()
    {
        string text = "123";

        var result = text.ToInt();

        Assert.Equal(123, result);
    }

    [Fact]
    public void ToInt_ReturnsFallback_WhenStringIsInvalid()
    {
        string text = "abc";

        var result = text.ToInt(99);

        Assert.Equal(99, result);
    }

    [Fact]
    public void ToInt_ReturnsZero_WhenStringIsInvalid_AndFallbackNotProvided()
    {
        string text = "abc";

        var result = text.ToInt();

        Assert.Equal(0, result);
    }

    [Fact]
    public void ToInt_ReturnsFallback_WhenStringIsNull()
    {
        string? text = null;

        var result = text.ToInt(42);

        Assert.Equal(42, result);
    }

    [Fact]
    public void ToInt_ReturnsFallback_WhenStringIsEmpty()
    {
        string text = string.Empty;

        var result = text.ToInt(7);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ToInt_ParsesNegativeNumbers()
    {
        string text = "-15";

        var result = text.ToInt();

        Assert.Equal(-15, result);
    }

    #endregion
}
