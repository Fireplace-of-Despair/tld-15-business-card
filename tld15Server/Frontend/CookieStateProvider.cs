// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Serilog;
using tld15Server.Composition;
using tld15Server.Features.Identity;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15Server.Frontend;

public class CookieStateProvider(
      IHttpContextAccessor httpContextAccessor
    , IMediator mediator
    , CacheManager cacheManager)
    : AuthenticationStateProvider
{
    private readonly AuthenticationState _anonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return BuildAuthenticationStateAsync();
    }

    private async Task<AuthenticationState> BuildAuthenticationStateAsync()
    {
        if (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
        {
            // No context of its own: an interactive circuit reads the state it was started with,
            // and nothing here signs anybody out.
            return _anonymousState;
        }

        var cookieSession = (httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity)?
            .Claims.FirstOrDefault(x => x.Type == Globals.CustomClaim.Session);

        if (!Guid.TryParse(cookieSession?.Value ?? string.Empty, out var sessionId))
        {
            return await SignOutAsync(null, "the principal carries no session claim");
        }

        var session = cacheManager.GetSessionById(sessionId);
        if (session == null)
        {
            return await SignOutAsync(sessionId, "the cache holds no session under that id: it expired, it was wiped, or the application restarted");
        }

        if (httpContextAccessor.HttpContext?.User is not ClaimsPrincipal identity)
        {
            return await SignOutAsync(sessionId, "the context carries no principal");
        }

        var features = FeatureMismatch(httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity, session);
        if (features != null)
        {
            return await SignOutAsync(sessionId, features);
        }

        var metadata = SessionMetadata.Mismatch(httpContextAccessor.HttpContext, session);
        if (metadata != null)
        {
            return await SignOutAsync(sessionId, metadata);
        }

        return new AuthenticationState(identity);
    }

    public async Task<bool> AuthenticateUserAsync(List<string> roles, Guid sessionId)
    {
        if (roles.Count == 0) { return false; }

        var claims = new List<Claim>();
        claims.AddRange(roles.Select(role => new Claim(Globals.CustomClaim.Feature, role)));
        claims.Add(new Claim(Globals.CustomClaim.Session, sessionId.ToString()));

        var identity = new ClaimsIdentity(claims, Globals.Cookie.Identity);
        var principal = new ClaimsPrincipal(identity);

        if (httpContextAccessor.HttpContext != null)
        {
            await httpContextAccessor.HttpContext.SignInAsync(principal);
        }

        NotifyAuthenticationStateChanged(BuildAuthenticationStateAsync());
        return true;
    }

    public async Task SignOutAsync()
    {
        if (httpContextAccessor.HttpContext == null) { return; }

        var cookieSession = (httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity)?
            .Claims.FirstOrDefault(x => x.Type == Globals.CustomClaim.Session);

        if (Guid.TryParse(cookieSession?.Value ?? string.Empty, out var sessionId))
        {
            await mediator.Send(new IdentityDeleteFeature.Query { SessionId = sessionId });
        }

        await httpContextAccessor.HttpContext!.SignOutAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymousState));
    }

    /// <summary>
    /// Ends the session and says why. This path deletes the session outright, so a silent one leaves
    /// nothing behind to explain the sign-out with.
    /// </summary>
    private async Task<AuthenticationState> SignOutAsync(Guid? sessionId, string reason)
    {
        Log.Warning("Session dropped while building the authentication state on {Method} {Path}, session {SessionId}: {Reason}",
            httpContextAccessor.HttpContext?.Request.Method,
            httpContextAccessor.HttpContext?.Request.Path.Value,
            sessionId,
            reason);

        await SignOutAsync();

        return _anonymousState;
    }

    private static string? FeatureMismatch(ClaimsIdentity? claimsIdentity, SharedSession session)
    {
        if (claimsIdentity == null) { return "the context carries no claims identity"; }

        var principalFeatures = claimsIdentity.Claims
            .Where(c => c.Type == Globals.CustomClaim.Feature)
            .Select(c => c.Value)
            .ToList();

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
