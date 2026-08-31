// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using StainlessCore;
using StainlessCore.Common.Helpers;
using tld15Server.Features.Identity;

namespace tld15Server.Frontend.Pages.Identity;

[AllowAnonymous]
public partial class LoginPage
{
    public const string Url = "/identity/login";

    [Inject] private NavigationManager navigation { get; set; } = default!;
    [Inject] private CookieStateProvider authStateProvider { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState> authenticationState { get; set; } = default!;

    public sealed class Request
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    [SupplyParameterFromForm(FormName = "form_login")]
    private Request formLogin { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        formLogin ??= new Request();

        if (QueryHelpers.ParseQuery(new Uri(navigation.Uri).Query).TryGetValue("returnUrl", out var returnUrlValue))
        {
            formLogin.ReturnUrl = returnUrlValue;
        }

        var authState = await authenticationState;
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            navigation.NavigateTo(formLogin.ReturnUrl ?? Home.Url);
        }
    }

    private async Task OnSubmit()
    {
        await Task.Delay(10, _cts.Token);

        var result = await Execute.Run(async () =>
        {
            var session = await Mediator.Send(new IdentityPostFeature.Command
            {
                Login = formLogin.Login,
                Password = formLogin.Password,
                UserAgent = HttpContextAccessor.GetUserAgent(),
                UserIP = HttpContextAccessor.GetRemoteIpAddress(),
                AcceptLanguage = HttpContextAccessor.GetAcceptLanguage(),
                AcceptEncoding = HttpContextAccessor.GetAcceptEncoding()
            }, _cts.Token);

            await authStateProvider.AuthenticateUserAsync(session.Features, session.Id);

            return session;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            return;
        }

        navigation.NavigateTo(formLogin.ReturnUrl ?? Home.Url);
    }
}
