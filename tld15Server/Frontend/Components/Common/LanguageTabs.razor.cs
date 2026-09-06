// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace tld15Server.Frontend.Components.Common;

/// <summary>
/// One tab per locale. Every editor that keeps a translation per locale shows the same strip, so an
/// editor learns it once, and a locale that carries nothing says so on its own tab.
/// </summary>
public partial class LanguageTabs
{
    /// <summary> The locales to show, as an id to a name. </summary>
    [Parameter] public Dictionary<string, string> Languages { get; set; } = [];

    /// <summary> The locale the strip is on. </summary>
    [Parameter] public string Selected { get; set; } = string.Empty;

    [Parameter] public EventCallback<string> SelectedChanged { get; set; }

    /// <summary>
    /// Whether a locale carries anything. Left unset, every tab reads as filled — a caller that
    /// cannot answer the question should not be asked to pretend it can.
    /// </summary>
    [Parameter] public Func<string, bool>? HasContent { get; set; }

    private Task SelectAsync(string languageId)
    {
        return SelectedChanged.InvokeAsync(languageId);
    }

    /// <summary> The name of a locale, marked when the locale holds nothing. </summary>
    private string Label(KeyValuePair<string, string> language)
    {
        return HasContent == null || HasContent(language.Key)
            ? language.Value
            : $"{language.Value} 〇";
    }
}
