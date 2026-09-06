// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using tld15Server.Composition;

namespace tld15Server.Frontend.Pages.Settings;

public partial class ChangeLanguagePage
{
    public const string Url = "/settings/language";

    public class LanguageModel
    {
        public string SelectedLanguage { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool HasBeenInitialized { get; set; }
    }

    [Inject] private NavigationManager navigation { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "language_form")]
    private LanguageModel formData { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        formData ??= new LanguageModel { HasBeenInitialized = false };

        if (formData.HasBeenInitialized) { return; }

        formData.SelectedLanguage = Language;
        formData.Name = Language;

        formData.HasBeenInitialized = true;
    }

    private async Task HandleSubmit()
    {
        if (HttpContextAccessor.HttpContext == null) { return; }

        HttpContextAccessor.HttpContext.Response.Cookies.Delete(Globals.Cookie.Language);

        HttpContextAccessor.HttpContext.Response.Cookies.Append
        (
              Globals.Cookie.Language
            , formData.SelectedLanguage, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                SameSite = SameSiteMode.Strict,
                Secure = true
            });

        navigation.NavigateTo(Home.Url);
    }
}
