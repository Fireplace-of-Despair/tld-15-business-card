// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using tld15Server.Frontend.Components.Icons;

namespace tld15Server.Common;

/// <summary>
/// Maps the key of a link onto the icon that stands for it. An icon is a Razor component under
/// <c>Frontend/Components/Icons</c>, so the page carries inline SVG and the icon takes its colour
/// from the element around it through <c>currentColor</c>.
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Every icon a link can carry, in the order the editor lists them. The table is the single
    /// source: <see cref="Names"/> reads the keys, <see cref="GetIcon"/> reads the fragments.
    /// </summary>
    private static readonly (string Key, RenderFragment Icon)[] _icons =
    [
        ("amazon", Render<Amazon>()),
        ("email", Render<Email>()),
        ("facebook", Render<Facebook>()),
        ("github", Render<Github>()),
        ("instagram", Render<Instagram>()),
        ("itch", Render<Itch>()),
        ("linkedin", Render<Linkedin>()),
        ("pirate", Render<Pirate>()),
        ("pixiv", Render<Pixiv>()),
        ("royalroad", Render<RoyalRoad>()),
        ("rss", Render<Rss>()),
        ("steam", Render<Steam>()),
        ("telegram", Render<Telegram>()),
        ("youtube", Render<Youtube>()),
    ];

    /// <summary>
    /// The hosts an icon is recognised by, for a caller that holds an address and nothing else. A
    /// row matches the host itself and any subdomain of it, so <c>fireplace-of-despair.itch.io</c>
    /// lands on the same icon as <c>itch.io</c>.
    /// </summary>
    private static readonly (string Host, string Key)[] _hosts =
    [
        ("amazon.com", "amazon"),
        ("facebook.com", "facebook"),
        ("github.com", "github"),
        ("instagram.com", "instagram"),
        ("itch.io", "itch"),
        ("linkedin.com", "linkedin"),
        ("pixiv.net", "pixiv"),
        ("royalroad.com", "royalroad"),
        ("steamcommunity.com", "steam"),
        ("steampowered.com", "steam"),
        ("t.me", "telegram"),
        ("telegram.me", "telegram"),
        ("telegram.org", "telegram"),
        ("youtube.com", "youtube"),
        ("youtu.be", "youtube"),

        // What this site hands out itself: the files a work offers for download live here and
        // nowhere else, so the host is enough to know what the button is.
        ("storage.fireplace-of-despair.org", "pirate"),
    ];

    private static readonly RenderFragment _unknown = Render<Unknown>();

    /// <summary> The keys an editor can pick from, in the order it shows them. </summary>
    public static IReadOnlyList<string> Names { get; } = [.. _icons.Select(x => x.Key)];

    public static string GetLanguage(string key)
    {
        var language = key.Split("_").LastOrDefault();

        if (string.IsNullOrEmpty(language) || language.Length > 3)
        {
            return "〇〇";
        }

        return language.ToUpper();
    }

    /// <summary> Get the icon part of a link key: everything ahead of the language suffix. </summary>
    public static string GetName(string key)
    {
        return key.Split("_")[0].ToLowerInvariant();
    }

    /// <summary>
    /// Get the markup of the icon a link key stands for, or a placeholder icon for a key this
    /// application does not know.
    /// </summary>
    public static RenderFragment GetIcon(string key)
    {
        var name = GetName(key);

        // A short linear pass beats a dictionary at this size, and it keeps the table above the
        // only place that names an icon.
        foreach (var icon in _icons)
        {
            if (icon.Key == name) { return icon.Icon; }
        }

        return _unknown;
    }

    /// <summary>
    /// The name of the icon an address stands for, or an empty string for an address this
    /// application recognises nothing in.
    /// </summary>
    /// <remarks>
    /// A relative address names no host and so names no icon: everything this reads is in the
    /// scheme, the host and the path of an absolute address.
    /// </remarks>
    public static string GetNameByUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) { return string.Empty; }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var address)) { return string.Empty; }

        // A mail address goes nowhere a host would explain, and the scheme already says what it is.
        if (string.Equals(address.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
        {
            return "email";
        }

        // A feed is told apart by what it serves rather than by where it is served from: it sits on
        // the site's own host, beside everything else that host carries.
        var path = address.AbsolutePath.TrimEnd('/');

        if (path.EndsWith("/rss", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/feed", StringComparison.OrdinalIgnoreCase))
        {
            return "rss";
        }

        var host = address.Host.ToLowerInvariant();

        if (host.StartsWith("www.", StringComparison.Ordinal)) { host = host[4..]; }

        foreach (var (candidate, key) in _hosts)
        {
            if (host == candidate || host.EndsWith($".{candidate}", StringComparison.Ordinal))
            {
                return key;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Get the markup of the icon an address stands for, or a placeholder icon for an address this
    /// application does not recognise.
    /// </summary>
    public static RenderFragment GetIconByUrl(string? url)
    {
        return GetIcon(GetNameByUrl(url));
    }

    /// <summary>
    /// Builds the fragment of a single icon component. The component is named at compile time, so
    /// nothing here reflects over types and the trimmer keeps exactly the icons this file lists.
    /// </summary>
    private static RenderFragment Render<TIcon>() where TIcon : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<TIcon>(0);
            builder.CloseComponent();
        };
    }
}
