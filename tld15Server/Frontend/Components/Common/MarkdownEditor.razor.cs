// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace tld15Server.Frontend.Components.Common;

/// <summary>
/// A markdown field with its preview. The preview renders through <see cref="MarkdownView"/>, which
/// is what the public pages render through, so an editor never approves something that will read
/// differently once it is published.
/// </summary>
public partial class MarkdownEditor
{
    [Inject] private IStringLocalizer<Localization.Resources> Localizer { get; set; } = default!;

    /// <summary> Which side of the text the field is showing. </summary>
    private enum Tab
    {
        /// <summary> The markdown itself, in a text area. </summary>
        Source = 0,

        /// <summary> The markdown as the site renders it. </summary>
        Preview = 1,
    }

    /// <summary> The markdown being edited. </summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary> How tall the field stands. </summary>
    [Parameter] public int Rows { get; set; } = 24;

    private Tab _tab = Tab.Source;

    private Task OnChangedAsync(ChangeEventArgs args)
    {
        return ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    }
}
