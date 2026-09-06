// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Collections.Generic;
using StainlessCore.Common.Helpers;

namespace tld15ServerTests.StainlessCores.UnitTests.Common.Helpers;

[Trait("Category", "StainlessCore")]
public class TranslationHelper_Tests
{
    private const string ErrorText = "ERROR CORE";

    [Fact]
    public void GetName_ReturnsMatchingTranslation_WhenLanguageExists()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("en", "Hello"),
                new("fr", "Bonjour"),
                new("es", "Hola")
            };

        var result = translations.GetName("fr");

        Assert.Equal("Bonjour", result);
    }

    [Fact]
    public void GetName_IsCaseInsensitive_WhenMatchingLanguage()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("EN", "Hello"),
                new("FR", "Bonjour")
            };

        var result = translations.GetName("fr");

        Assert.Equal("Bonjour", result);
    }

    [Fact]
    public void GetName_ReturnsFallback_WhenLanguageDoesNotExist()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("en", "Hello"),
                new("fr", "Bonjour")
            };

        var result = translations.GetName("de");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void GetName_ReturnsFallback_WhenMatchedValueIsWhitespace()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("en", "Hello"),
                new("fr", " ")
            };

        var result = translations.GetName("fr");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void GetName_ReturnsError_WhenCollectionIsEmpty()
    {
        var translations = new List<KeyValuePair<string, string>>();

        var result = translations.GetName("en");

        Assert.Equal(ErrorText, result);
    }

    [Fact]
    public void GetName_ReturnsError_WhenFallbackValueIsNullOrWhitespace()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("en", " "),
                new("fr", "")
            };

        var result = translations.GetName("de");

        Assert.Equal(ErrorText, result);
    }

    [Fact]
    public void GetName_ReturnsMatchedValue_EvenIfFallbackIsInvalid()
    {
        var translations = new List<KeyValuePair<string, string>>
            {
                new("en", " "),
                new("fr", "Bonjour")
            };

        var result = translations.GetName("fr");

        Assert.Equal("Bonjour", result);
    }

}
