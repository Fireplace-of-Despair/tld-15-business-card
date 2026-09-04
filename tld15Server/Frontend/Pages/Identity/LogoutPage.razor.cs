// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using StainlessCore;

namespace tld15Server.Frontend.Pages.Identity;

public partial class LogoutPage
{
    public const string Url = "/identity/logout";

    [Inject] private CookieStateProvider authStateProvider { get; set; } = default!;
    [Inject] private NavigationManager navigation { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var result = await Execute.Run(async () =>
        {
            await authStateProvider.SignOutAsync();
            navigation.NavigateTo(LoginPage.Url);

            return Task.CompletedTask;
        });
    }
}
