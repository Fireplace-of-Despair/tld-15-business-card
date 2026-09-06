// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Components;
using tld15Server.Common;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Components;

/// <summary>
/// One outward link drawn as its icon. A card and a reading page both show the same button, so the
/// markup and the rules that style it live here rather than beside either of them.
/// </summary>
/// <remarks>
/// The icon follows from the address: <see cref="IconHelper.GetIconByUrl"/> reads the host and an
/// address it does not recognise draws the placeholder icon. The language is a badge and nothing
/// more — a caller that has none, or one that holds something no language code looks like, gets a
/// button with no badge rather than a badge with nothing in it.
/// </remarks>
public partial class ExternalLinkIcon
{
    /// <summary> Where the link goes. </summary>
    [Parameter, EditorRequired] public string Url { get; set; } = string.Empty;

    /// <summary> The language the link speaks, or nothing when it speaks none this site can name. </summary>
    [Parameter] public string? Language { get; set; }

    private RenderFragment? _icon;

    /// <summary> The name of the icon, which is also what a screen reader is told the link is. </summary>
    private string _name = string.Empty;

    /// <summary> The badge, already in the case it is drawn in. Empty means no badge at all. </summary>
    private string _language = string.Empty;

    protected override void OnParametersSet()
    {
        _name = IconHelper.GetNameByUrl(Url);
        _icon = IconHelper.GetIconByUrl(Url);
        _language = ToBadge(Language);
    }

    /// <summary>
    /// The badge a language is drawn as. Nothing, blank, or anything that is not a language code
    /// this application stores comes back empty, and the button then carries the icon alone.
    /// </summary>
    private static string ToBadge(string? language)
    {
        var trimmed = language?.Trim() ?? string.Empty;

        return trimmed.Length > 0 && SharedLink.IsLanguageValid(trimmed)
            ? trimmed.ToUpperInvariant()
            : string.Empty;
    }
}
