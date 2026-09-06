// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// One row of a table of links: the address it opens and the language it speaks. A stored set of
/// links is keyed by the address itself, so an owner of links only has to store a url to a language.
/// </summary>
/// <remarks>
/// Nothing here names an icon. The icon a link draws is read off the address by
/// <see cref="Common.IconHelper"/>, so the site it leads to is never stored a second time beside it.
/// </remarks>
public class SharedLink
{
    /// <summary> The language of the link itself. Empty is allowed: a card then shows no badge. </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary> The address the link opens, which is also the key it is stored under. </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary> What an editor shows in place of a language nobody typed. </summary>
    public const string LanguageUnknown = "〇〇";

    /// <summary> Read a stored entry back into a row of an editor. </summary>
    public static SharedLink FromStored(string url, string language)
    {
        return new SharedLink { Url = url, Language = language };
    }


    /// <summary>
    /// Whether the language reads as a code this application stores: latin letters, at most three of
    /// them, following the reference table. Empty passes on purpose — a link without a language is a
    /// link the card draws with no badge at all.
    /// </summary>
    public static bool IsLanguageValid(string language)
    {
        return language.Length <= 3 && language.All(char.IsAsciiLetter);
    }

    /// <summary>
    /// The address in the form it is stored. A bare mail address is not something a browser can
    /// follow — it reads as a relative path — so it takes the mailto scheme. An address that already
    /// names a scheme is left exactly as it was typed.
    /// </summary>
    public static string ToStoredUrl(string url)
    {
        var trimmed = url.Trim();

        return IsBareMailAddress(trimmed) ? $"mailto:{trimmed}" : trimmed;
    }

    /// <summary> Whether the value is a mail address and nothing else. </summary>
    private static bool IsBareMailAddress(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);

        // Nothing to send to, or nothing to send it at.
        if (at <= 0 || at == value.Length - 1) { return false; }

        // A scheme, a path, a second at sign or a space: the value is an address of another shape,
        // and guessing at it would do more harm than leaving it alone.
        if (value.IndexOf('@', at + 1) >= 0) { return false; }
        if (value.Contains(':', StringComparison.Ordinal)) { return false; }
        if (value.Contains('/', StringComparison.Ordinal)) { return false; }
        if (value.Any(char.IsWhiteSpace)) { return false; }

        // A host with no dot in it is not a host a mail server delivers to.
        return value.IndexOf('.', at + 1) > at + 1;
    }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static Dictionary<string, string> JsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json)) { return []; }


        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _options) ?? [];
    }

    public static string? DictionaryToJson(Dictionary<string, string> links)
    {
        if (links.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(links, _options);
    }
}
