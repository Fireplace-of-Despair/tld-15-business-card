// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using tld15Server.Composition;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15Server.Frontend;

public class CookieEvent(CacheManager cacheManager) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var hasSessionId = Guid.TryParse(context.Principal?.FindFirstValue(Globals.CustomClaim.Session) ?? string.Empty, out var sessionId);
        if (!hasSessionId)
        {
            await Logout(context, null);
            return;
        }

        var session = cacheManager.GetSessionById(sessionId);
        if (session == null)
        {
            await Logout(context, sessionId);
            return;
        }

        if (!AreFeaturesEqual(context, session))
        {
            await Logout(context, sessionId);
            return;
        }

        if (!AreMetadataEqual(context, session))
        {
            await Logout(context, sessionId);
            return;
        }

        await base.ValidatePrincipal(context);
    }

    private async Task Logout(CookieValidatePrincipalContext context, Guid? sessionId)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (sessionId != null)
        {
            cacheManager.RemoveSessionById(sessionId.Value);
        }
    }

    private static bool AreFeaturesEqual(CookieValidatePrincipalContext context, SharedSession session)
    {
        var principalFeatures = context.Principal?.Claims
            .Where(c => c.Type == Globals.CustomClaim.Feature)
            .Select(c => c.Value)
            .ToList() ?? [];

        return principalFeatures.Count == session.Features.Count
            && principalFeatures.All(session.Features.Contains)
            && session.Features.All(principalFeatures.Contains);
    }

    private static bool AreMetadataEqual(CookieValidatePrincipalContext context, SharedSession session)
    {
        var request = context.HttpContext.Request;
        var userAgent = request.Headers.UserAgent.ToString();
        var userIP = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var acceptLanguage = request.Headers.AcceptLanguage.ToString();
        var acceptEncoding = request.Headers.AcceptEncoding.ToString();

        return string.Equals(session.UserAgent, userAgent, StringComparison.Ordinal)
            && string.Equals(session.UserIP, userIP, StringComparison.Ordinal)
            && string.Equals(session.AcceptLanguage, acceptLanguage, StringComparison.Ordinal)
            && string.Equals(session.AcceptEncoding, acceptEncoding, StringComparison.Ordinal);
    }
}
