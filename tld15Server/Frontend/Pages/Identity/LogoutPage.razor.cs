// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
