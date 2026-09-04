// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Globalization;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Endpoints;
using StainlessCore.Exceptions;
using StainlessGenerated;
using tld15Server.Endpoints;
using tld15Server.Frontend;
using tld15Server.Frontend.Pages.Identity;
using tld15Server.Services;

namespace tld15Server.Composition;

public static class ServiceInjection
{
    public static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediator();

        return builder;
    }
    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var cookieLifetime = configuration.GetValue<int>(Globals.Security.CookieExpiration);
        var cookieMaxAge = configuration.GetValue<int>(Globals.Security.CookieMaxAge);

        builder.Services.AddScoped<CookieStateProvider>();
        builder.Services.AddScoped<CookieEvent>();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options =>
        {
            options.Cookie.Name = Globals.Cookie.Auth;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.IsEssential = true;

            options.LoginPath = LoginPage.Url.ToLower();
            options.LogoutPath = LogoutPage.Url.ToLower();
            options.AccessDeniedPath = "/access-denied";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieLifetime);
            options.SlidingExpiration = true;
            options.Cookie.MaxAge = TimeSpan.FromMinutes(cookieMaxAge);

            options.EventsType = typeof(CookieEvent);
        });

        foreach (var featureId in Registry.FeatureIds)
        {
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(featureId, policy => policy.RequireClaim("Feature", featureId));
        }

        return builder;
    }
    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var windowSeconds = configuration.GetValue<int>(Globals.Security.RateLimitWindowSeconds);
        var globalPermits = configuration.GetValue<int>(Globals.Security.RateLimitGlobalPermits);
        var apiPermits = configuration.GetValue<int>(Globals.Security.RateLimitApiPermits);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = IncidentCode.TooManyAttempts.ToHTTPCode();

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => BuildPartition(context, globalPermits, windowSeconds));

            // The protected group carries this policy as well, so a sync client meets both limits.
            options.AddPolicy(Globals.RateLimit.ApiPolicy,
                context => BuildPartition(context, apiPermits, windowSeconds));

            options.OnRejected = WriteRejectionAsync;
        });

        return builder;
    }

    public static WebApplicationBuilder AddHostedService(this WebApplicationBuilder builder)
    {
        return builder;
    }
    public static WebApplicationBuilder AddGeneral(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CacheManager>();
        return builder;
    }

    public static void AddEndpoints(this WebApplication app)
    {
        Registry.MapPublic(app
            .MapGroup("/api/public")
            .AddEndpointFilter<ExceptionHandlingFilter>());

        // ExceptionHandlingFilter registers first, so it wraps ApiKeyFilter. An IncidentException from
        // the key check then reaches the client as an Incident.
        Registry.MapProtected(app
            .MapGroup("/api/protected")
            .AddEndpointFilter<ExceptionHandlingFilter>()
            .AddEndpointFilter<ApiKeyFilter>()
            .RequireRateLimiting(Globals.RateLimit.ApiPolicy));

        Registry.MapExternal(app
            .MapGroup("/api/external")
            .AddEndpointFilter<ExceptionHandlingFilter>());
    }

    /// <summary> Build the limiter partition of one caller address </summary>
    private static RateLimitPartition<string> BuildPartition(HttpContext context, int permits, int windowSeconds)
    {
        // UseForwardedHeaders runs first in the pipeline, so this address is the caller, not Caddy. A
        // wrong Security:ForwardedHeaders:KnownNetworks puts every caller into one partition. A single
        // busy client then rejects all the others.
        var partition = context.Connection.RemoteIpAddress?.ToString() ?? Globals.RateLimit.UnknownPartition;

        if (permits <= Globals.RateLimit.Disabled || windowSeconds <= 0)
        {
            return RateLimitPartition.GetNoLimiter(partition);
        }

        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,

            // The limiter reports Retry-After only when it replenishes on its own.
            AutoReplenishment = true,
        });
    }

    // The rejection carries the envelope of ExceptionHandlingFilter.Result<T>, so an API client reads it
    // with the parser that it already uses for an incident. The limiter runs as middleware, ahead of the
    // endpoint filter, so the filter cannot write this answer.
    // Three '$' characters on purpose: the body ends with two closing braces, and a shorter prefix reads
    // them as the end of an interpolation.
    private static readonly string _rejectionBody =
        $$$"""{"incident":{"code":{{{(int)IncidentCode.TooManyAttempts}}},"description":"{{{IncidentCode.TooManyAttempts}}}"}}""";

    /// <summary> Answer a caller that passed the limit </summary>
    private static ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ctn)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = IncidentCode.TooManyAttempts.ToHTTPCode();

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        // Only an API caller reads the envelope. A browser gets one line of text.
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            response.ContentType = "text/plain";
            return new ValueTask(response.WriteAsync("Too many requests.", ctn));
        }

        response.ContentType = "application/json";
        return new ValueTask(response.WriteAsync(_rejectionBody, ctn));
    }
}
