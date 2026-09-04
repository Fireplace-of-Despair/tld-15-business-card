// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace tld15Server.Frontend.Components.Common;

public partial class NavAccordion
{
    private bool _isExpanded;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [Parameter] public string[] ActiveUrls { get; set; } = [];
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    protected override void OnInitialized()
    {
        if (ActiveUrls == null || ActiveUrls.Length == 0)
        {
            _isExpanded = false;
            return;
        }

        _isExpanded = ActiveUrls.Any(url =>
        {
            var absoluteUrl = NavManager.ToAbsoluteUri(url).AbsoluteUri;
            if (Match == NavLinkMatch.Prefix)
            {
                return NavManager.Uri.StartsWith(absoluteUrl, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.Equals(absoluteUrl, NavManager.Uri, StringComparison.OrdinalIgnoreCase);
            }
        });
    }
}
