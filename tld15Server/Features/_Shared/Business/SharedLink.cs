// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// One row of a link dictionary: an icon, the language the link speaks, and the address it opens.
/// The stored key holds the icon and the language in the <c>%key-name%_%language%</c> format the
/// pages read back, so an owner of links only has to store a key to a url.
/// </summary>
public class SharedLink
{
    /// <summary> The icon of the link, as <see cref="Frontend.Components.Common.IconHelper"/> names it. </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary> The language of the link itself. Empty is allowed: a card then shows a placeholder badge. </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary> The address the link opens. </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary> Build the key this link is stored under. </summary>
    public static string ToKey(string icon, string language)
    {
        return $"{icon}_{language}";
    }

    /// <summary>
    /// Split a stored key. The icon ends at the first separator and the language takes the rest,
    /// which is what <c>IconHelper</c> assumes when it picks the icon of a key.
    /// </summary>
    public static (string Icon, string Language) SplitKey(string key)
    {
        var separator = key.IndexOf('_', StringComparison.Ordinal);

        return separator < 0
            ? (key, string.Empty)
            : (key[..separator], key[(separator + 1)..]);
    }

    /// <summary> Read a stored entry back into a row of an editor. </summary>
    public static SharedLink FromStored(string key, string url)
    {
        var (icon, language) = SplitKey(key);

        return new SharedLink { Icon = icon, Language = language, Url = url };
    }

    /// <summary>
    /// Whether the language reads as a code this application stores: latin letters, at most three of
    /// them, following the reference table. Empty passes on purpose — a link without a language is a
    /// link the card badges with a placeholder instead of a code.
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
}

/// <summary>
/// A link that lives inside one translation of a content. The locales of a content keep separate
/// dictionaries, so the row has to name the one it lands in.
/// </summary>
public sealed class SharedContentLink : SharedLink
{
    /// <summary> The translation that carries the link: the locale of the content_translation row. </summary>
    public string TranslationLanguageId { get; set; } = string.Empty;

    /// <summary> Read a stored entry of one translation back into a row of the editor. </summary>
    public static SharedContentLink FromStoredOf(string translationLanguageId, string key, string url)
    {
        var (icon, language) = SplitKey(key);

        return new SharedContentLink
        {
            TranslationLanguageId = translationLanguageId,
            Icon = icon,
            Language = language,
            Url = url,
        };
    }
}
