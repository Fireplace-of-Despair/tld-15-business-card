// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Text;

namespace tld15Server.Services;

/// <summary>
/// What this application will let a browser follow. Stored text and stored addresses arrive from an
/// editor, and every place that puts one into markup asks here first, so the answer is the same
/// everywhere and lives in one file.
/// </summary>
public static class UrlPolicy
{
    /// <summary> Schemes a reader may be sent to. </summary>
    private static readonly string[] _followable = ["http", "https", "mailto"];

    /// <summary> Schemes a picture may be loaded from. A mail address is not a picture. </summary>
    private static readonly string[] _image = ["http", "https"];

    /// <summary> The address with its control characters removed and its ends trimmed. </summary>
    /// <remarks>
    /// A tab or a newline inside a scheme is not something a reader typed on purpose, and a browser
    /// drops it before it follows the address. The check has to read what the browser will read.
    /// </remarks>
    public static string Clean(string? url)
    {
        if (string.IsNullOrEmpty(url)) { return string.Empty; }

        var builder = new StringBuilder(url.Length);

        foreach (var character in url)
        {
            if (!char.IsControl(character)) { builder.Append(character); }
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// The scheme of an address, or an empty string when it carries none. A colon that follows a
    /// slash, a query or a fragment belongs to the path, not to a scheme.
    /// </summary>
    public static string SchemeOf(string url)
    {
        var colon = url.IndexOf(':');

        if (colon <= 0) { return string.Empty; }

        var boundary = url.AsSpan().IndexOfAny('/', '?', '#');

        return (boundary >= 0 && boundary < colon) ? string.Empty : url[..colon];
    }

    /// <summary> Whether a link may keep its address. An address with no scheme stays inside the site. </summary>
    public static bool IsFollowable(string url)
    {
        var scheme = SchemeOf(url);

        return scheme.Length == 0 || Contains(_followable, scheme);
    }

    /// <summary> Whether a picture may be loaded from the address. </summary>
    public static bool IsImageSource(string url)
    {
        var scheme = SchemeOf(url);

        return scheme.Length == 0 || Contains(_image, scheme);
    }

    /// <summary>
    /// The scheme and host of an address, with nothing after them, or an empty string when the
    /// address carries no host of its own.
    /// </summary>
    public static string OriginOf(string? url)
    {
        var cleaned = Clean(url);

        if (cleaned.Length == 0 || !IsImageSource(cleaned)) { return string.Empty; }

        return Uri.TryCreate(cleaned, UriKind.Absolute, out var parsed) && parsed.IsAbsoluteUri
            ? $"{parsed.Scheme}://{parsed.Authority}"
            : string.Empty;
    }

    /// <summary>
    /// The host worth opening a connection to before the page needs one, read off the first of the
    /// given addresses that names a host of its own. An empty answer means there is nothing to hint
    /// at: the pictures are on this site, or there are none.
    /// </summary>
    /// <param name="origin"> Where this site answers, so its own host is not hinted at twice. </param>
    /// <param name="candidates"> The pictures the page will draw, in the order it draws them. </param>
    public static string PreconnectFor(string origin, params string?[] candidates)
    {
        var self = OriginOf(origin);

        foreach (var candidate in candidates)
        {
            var host = OriginOf(candidate);

            if (host.Length == 0) { continue; }

            // A connection to this site is already open by the time the head is read.
            if (string.Equals(host, self, StringComparison.OrdinalIgnoreCase)) { return string.Empty; }

            return host;
        }

        return string.Empty;
    }

    private static bool Contains(string[] schemes, string scheme)
    {
        return Array.Exists(schemes, x => string.Equals(x, scheme, StringComparison.OrdinalIgnoreCase));
    }
}
