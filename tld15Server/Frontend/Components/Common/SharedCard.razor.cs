// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Components;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Components.Common;

public partial class SharedCard
{
    [Parameter]
    public required SharedCardPreview SharedCardContent { get; set; }

    /// <summary> Which heading level the title of the card takes. </summary>
    [Parameter] public int HeadingLevel { get; set; } = 3;

    internal string _url = string.Empty;

    /// <summary> Whether the card leads off this site, which decides how its links behave. </summary>
    internal bool _external;

    protected override void OnParametersSet()
    {
        _external = !string.IsNullOrWhiteSpace(SharedCardContent.ExternalUrl);

        // A project and an article are the same row split by a type and are read on the same page.
        // Anything that names an address of its own is read wherever that address leads.
        _url = _external
            ? SharedCardContent.ExternalUrl!
            : $"{Pages.Projects.ProjectReadPage.Url}/{SharedCardContent.Id}";
    }
}

