// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace tld15Server.Frontend.Navigations;

/// <summary>
/// A row of links to the blocks of the page it stands on. A page that scrolls for a while gives a
/// reader somewhere to jump from, and the links are plain anchors: no script runs and the whole row
/// is in the first response.
/// </summary>
/// <remarks>
/// The caller names only the blocks it actually renders, so the row never points at a heading that
/// is not on the page.
/// </remarks>
public partial class LocalNavigation
{
    /// <summary> One block of the page. </summary>
    /// <param name="Id"> The id of the heading the link jumps to. </param>
    /// <param name="Title"> What the link says. </param>
    public sealed record Section(string Id, string Title);

    [Inject] private IStringLocalizer<Localization.Resources> Localizer { get; set; } = default!;

    /// <summary> The blocks to offer, in the order the page renders them. </summary>
    [Parameter] public IReadOnlyList<Section> Sections { get; set; } = [];
}
