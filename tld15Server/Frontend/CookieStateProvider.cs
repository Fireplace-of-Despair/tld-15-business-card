// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
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
            return _anonymousState;
        }

        var cookieSession = (httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity)?
            .Claims.FirstOrDefault(x => x.Type == Globals.CustomClaim.Session);

        if (!Guid.TryParse(cookieSession?.Value ?? string.Empty, out var sessionId))
        {
            await SignOutAsync();
            return _anonymousState;
        }

        var session = cacheManager.GetSessionById(sessionId);
        if (session == null)
        {
            await SignOutAsync();
            return _anonymousState;
        }

        if (httpContextAccessor.HttpContext?.User is not ClaimsPrincipal identity)
        {
            await SignOutAsync();
            return _anonymousState;
        }

        if (!AreFeaturesEqual(httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity, session))
        {
            await SignOutAsync();
            return _anonymousState;
        }

        if (!AreMetadataEqual(httpContextAccessor.HttpContext, session))
        {
            await SignOutAsync();
            return _anonymousState;
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

    private static bool AreFeaturesEqual(ClaimsIdentity? claimsIdentity, SharedSession session)
    {
        if (claimsIdentity == null) { return false; }

        var principalFeatures = claimsIdentity.Claims
            .Where(c => c.Type == Globals.CustomClaim.Feature)
            .Select(c => c.Value)
            .ToList() ?? [];

        return principalFeatures.Count == session.Features.Count
            && principalFeatures.All(session.Features.Contains)
            && session.Features.All(principalFeatures.Contains);
    }

    private static bool AreMetadataEqual(HttpContext? context, SharedSession session)
    {
        if (context == null) { return false; }

        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userIP = context.Connection.RemoteIpAddress?.ToString();
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();
        var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();

        return string.Equals(session.UserAgent, userAgent, StringComparison.Ordinal)
            && string.Equals(session.UserIP, userIP, StringComparison.Ordinal)
            && string.Equals(session.AcceptLanguage, acceptLanguage, StringComparison.Ordinal)
            && string.Equals(session.AcceptEncoding, acceptEncoding, StringComparison.Ordinal);
    }
}
