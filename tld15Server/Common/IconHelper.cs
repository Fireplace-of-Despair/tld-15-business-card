// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
