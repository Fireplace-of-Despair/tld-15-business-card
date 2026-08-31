// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace tld15Server.Frontend.Components.Common.LocalTime;

public sealed class InitializeBrowserTime : ComponentBase
{
    [Inject] private BrowserTimeProvider TimeProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender
            || TimeProvider is not BrowserTimeProvider browserTimeProvider
            || browserTimeProvider.IsLocalTimeZoneSet)
        {
            return;
        }

        try
        {
            await using var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./lib/timezone.js");
            var timeZone = await module.InvokeAsync<string>("getBrowserTimeZone");
            browserTimeProvider.SetBrowserTimeZone(timeZone);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
