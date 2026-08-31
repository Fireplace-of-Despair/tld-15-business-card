// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using StainlessCore.Common.Helpers;
using StainlessCore.Exceptions;
using tld15Server.Composition;
using tld15Server.Frontend.Components.Common.LocalTime;
using tld15Server.Frontend.Components.Navigations;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages;

public abstract class BasePage : ComponentBase, IDisposable
{
    [Inject] internal IStringLocalizer<Localization.Resources> Localizer { get; set; } = default!;
    [Inject] protected IMediator Mediator { get; set; } = default!;
    [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
    [Inject] protected CookieStateProvider CookieStateProvider { get; set; } = default!;
    [Inject] protected CacheManager CacheManager { get; set; } = default!;
    [Inject] protected NavigationState NavigationState { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected BrowserTimeProvider TimeProvider { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    internal static string Language
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;

            return Globals.Locales.ContainsKey(culture) ? culture : Globals.Locales.First().Key;
        }
    }
    internal IncidentCode? IncidentCode { get; set; } = null;
    internal bool IsLoading { get; set; } = true;
    internal CancellationTokenSource _cts = new();

    protected override void OnInitialized()
    {
        NavigationState.OnChange += StateHasChanged;
        CookieStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        TimeProvider.LocalTimeZoneChanged += OnLocalTimeZoneChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnLocalTimeZoneChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    protected string LocalTime(DateTimeOffset value, string format)
    {
        return value.ToTimeProviderDateTime(TimeProvider).ToString(format, CultureInfo.CurrentCulture);
    }

    protected async Task<bool> HasFeatureAsync(string featureId)
    {
        if (AuthenticationStateTask == null) { return false; }

        var state = await AuthenticationStateTask;

        return state.User.HasClaim(Globals.CustomClaim.Feature, featureId);
    }

    internal async Task<Guid?> GetAccountIdAsync()
    {
        var authState = await CookieStateProvider.GetAuthenticationStateAsync();

        var session = authState.User.Claims.FirstOrDefault(c => c.Type == Globals.CustomClaim.Session);
        if (session == null) { return null; }

        if (!Guid.TryParse(session.Value, out var sessionId)) { return null; }

        return CacheManager.GetSessionById(sessionId)?.AccountId;
    }

    internal async Task<bool> ConfirmAsync(string message)
    {
        try
        {
            return await JS.InvokeAsync<bool>("confirm", message);
        }
        catch (JSDisconnectedException)
        {
            // circuit died before the browser answered: nothing was confirmed, so treat it as a decline
            return false;
        }
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
            var cts = Interlocked.Exchange(ref _cts, null!);
            if (cts != null)
            {
                NavigationState.OnChange -= StateHasChanged;
                CookieStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
                TimeProvider.LocalTimeZoneChanged -= OnLocalTimeZoneChanged;
                cts.Cancel();
                cts.Dispose();
            }
        }
    }

    #endregion
}
