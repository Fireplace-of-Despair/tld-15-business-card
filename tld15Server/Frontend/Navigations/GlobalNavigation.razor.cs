// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using tld15Server.Frontend.Pages;

namespace tld15Server.Frontend.Navigations;

/// <summary>
/// The pages a visitor moves between, under the brand. The set is short and fixed, so it is named
/// here rather than read from anywhere: a page that is not in this list is not a page a visitor is
/// meant to find on their own.
/// </summary>
public partial class GlobalNavigation
{
    /// <summary> One page of the site. </summary>
    /// <param name="Url"> Where the link goes, as the page declares it. </param>
    /// <param name="Title"> What the link says. </param>
    private sealed record Entry(string Url, string Title);

    [Inject] private IStringLocalizer<Localization.Resources> Localizer { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string _path = string.Empty;

    private List<Entry> Entries =>
    [
        new(Home.Url, Localizer["Navigation.Home"].Value),
        new(Pages.Presses.PressPage.Url, Localizer["Press"].Value),
        new(Pages.Archive.ArchivePage.Url, Localizer["Archive"].Value),
    ];

    protected override void OnParametersSet()
    {
        // The path of the page being rendered, so the link that points at it can say so. A query or
        // a fragment does not change which page a reader is on.
        var relative = Navigation.ToBaseRelativePath(Navigation.Uri);
        var end = relative.IndexOfAny(['?', '#']);

        _path = "/" + (end < 0 ? relative : relative[..end]).TrimEnd('/');
    }
}
