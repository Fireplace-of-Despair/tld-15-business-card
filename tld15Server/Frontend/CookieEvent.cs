// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
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
            await Logout(context, null, "the principal carries no session claim");
            return;
        }

        var session = cacheManager.GetSessionById(sessionId);
        if (session == null)
        {
            await Logout(context, sessionId, "the cache holds no session under that id: it expired, it was wiped, or the application restarted");
            return;
        }

        var features = FeatureMismatch(context, session);
        if (features != null)
        {
            await Logout(context, sessionId, features);
            return;
        }

        var metadata = SessionMetadata.Mismatch(context.HttpContext, session);
        if (metadata != null)
        {
            await Logout(context, sessionId, metadata);
            return;
        }

        await base.ValidatePrincipal(context);
    }

    private async Task Logout(CookieValidatePrincipalContext context, Guid? sessionId, string reason)
    {
        // A session that ends without a word in the log is a session nobody can account for later.
        Log.Warning("Session rejected on {Method} {Path}, session {SessionId}: {Reason}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path.Value,
            sessionId,
            reason);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (sessionId != null)
        {
            cacheManager.RemoveSessionById(sessionId.Value);
        }
    }

    private static string? FeatureMismatch(CookieValidatePrincipalContext context, SharedSession session)
    {
        var principalFeatures = context.Principal?.Claims
            .Where(c => c.Type == Globals.CustomClaim.Feature)
            .Select(c => c.Value)
            .ToList() ?? [];

        if (principalFeatures.Count == session.Features.Count
            && principalFeatures.All(session.Features.Contains)
            && session.Features.All(principalFeatures.Contains))
        {
            return null;
        }

        return $"the features of the cookie differ from the features of the session: "
            + $"cookie [{string.Join(", ", principalFeatures.Order())}], "
            + $"session [{string.Join(", ", session.Features.Order())}]";
    }
}
