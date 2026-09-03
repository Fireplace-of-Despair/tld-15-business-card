// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
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

        // The same address answers with a different document depending on this cookie, so anything
        // that caches by address alone - a proxy, a CDN, the browser - has to be told, or a reader
        // is handed the other language.
        //
        // Only when the answer is a page. The content type is not known until the response starts,
        // which is why this waits: a fingerprinted asset varies by nothing, and Vary: Cookie would
        // cost it a shared cache entry for no gain.
        context.Response.OnStarting(static state =>
        {
            var response = ((HttpContext)state).Response;

            if (response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
            {
                response.Headers.Append(HeaderNames.Vary, HeaderNames.Cookie);
            }

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
