// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using tld15Server.Composition;

namespace tld15Server.Frontend.Layout;

public partial class MainLayout
{
    [Inject] private IStringLocalizer<Localization.Resources> Localizer { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary> Whether the page being rendered is one that manages the site. </summary>
    private bool IsAdmin => ("/" + Navigation.ToBaseRelativePath(Navigation.Uri))
        .StartsWith(Globals.Route.Admin, StringComparison.OrdinalIgnoreCase);

    private string SourceUrl
    {
        get
        {
            return Configuration[Globals.Settings.SourceUrl]!;
        }
    }
}
