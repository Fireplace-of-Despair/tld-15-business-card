// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using StainlessCore.Common.Helpers;
using tld15Server.Frontend.Components.Common.LocalTime;

namespace tld15Server.Frontend.Components.Common;

public class BrowserTime : ComponentBase, IDisposable
{
    [Inject] private BrowserTimeProvider TimeProvider { get; set; } = default!;
    [Parameter] public DateTimeOffset DateTimeOffset { get; set; }
    [Parameter] public string? Format { get; set; }

    protected override void OnInitialized()
    {
        if (TimeProvider is BrowserTimeProvider browserTimeProvider)
        {
            browserTimeProvider.LocalTimeZoneChanged += LocalTimeZoneChanged;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, DateTimeOffset.ToTimeProviderDateTime(TimeProvider).ToString(Format ?? string.Empty));
    }

    private void LocalTimeZoneChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }


    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TimeProvider is BrowserTimeProvider browserTimeProvider)
            {
                browserTimeProvider.LocalTimeZoneChanged -= LocalTimeZoneChanged;
            }
        }
    }

    #endregion
}
