// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Http;
using StainlessCore.Composition;

namespace StainlessCore.Common.Helpers;

/// <summary>Helper for <see cref="IHttpContextAccessor"/> </summary>
public static class HttpContextAccessorHelper
{
    /// <summary>Extract user agent</summary>
    /// <param name="http"><see cref="IHttpContextAccessor"/></param>
    /// <returns>User agent or <c>"ERROR"</c> </returns>
    public static string GetUserAgent(this IHttpContextAccessor http)
    {
        return http.HttpContext?.Request.Headers.UserAgent.ToString() ?? Globals.Error.Text;
    }

    /// <summary>Extract user IP address</summary>
    /// <param name="http"><see cref="IHttpContextAccessor"/></param>
    /// <returns>User IP address or <c>"ERROR"</c> </returns>
    public static string GetRemoteIpAddress(this IHttpContextAccessor http)
    {
        return http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? Globals.Error.Text;
    }

    /// <summary>Extract user AcceptLanguage</summary>
    /// <param name="http"><see cref="IHttpContextAccessor"/></param>
    /// <returns>User AcceptLanguage or <c>"ERROR"</c> </returns>
    public static string GetAcceptLanguage(this IHttpContextAccessor http)
    {
        return http.HttpContext?.Request.Headers.AcceptLanguage.ToString() ?? Globals.Error.Text;
    }

    /// <summary>Extract user AcceptEncoding</summary>
    /// <param name="http"><see cref="IHttpContextAccessor"/></param>
    /// <returns>User AcceptEncoding or <c>"ERROR"</c> </returns>
    public static string GetAcceptEncoding(this IHttpContextAccessor http)
    {
        return http.HttpContext?.Request.Headers.AcceptEncoding.ToString() ?? Globals.Error.Text;
    }
}
