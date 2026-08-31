// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using tld15Server.Composition;

namespace tld15Server.Frontend.Localization;

public sealed class LocalizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userLanguage = context.Request.Cookies[Globals.Cookie.Language];

        if (string.IsNullOrWhiteSpace(userLanguage) || !Globals.Locales.ContainsKey(userLanguage))
        {
            userLanguage = Globals.LanguageFallback;
        }

        var culture = CultureInfo.GetCultureInfo(userLanguage);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await next(context);
    }
}
