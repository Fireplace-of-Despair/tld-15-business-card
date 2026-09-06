// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
