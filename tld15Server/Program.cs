// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StainlessCore.Composition;
using StainlessInfrastructure;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Migrations;
using tld15Server.Composition;
using tld15Server.Frontend;
using tld15Server.Frontend.Components.Common.LocalTime;
using tld15Server.Frontend.Components.Navigations;
using tld15Server.Frontend.Localization;
using tld15Server.Services;

namespace tld15Server;

[ExcludeFromCodeCoverage]
public sealed class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    public static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        var connectionString = builder.Configuration.GetConnectionString(Globals.Settings.ConnectionString)!;

        //Interactive Blazor will wait 30s for sokets to close
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(1));
        }

        builder.Services
               .AddRazorComponents()
               .AddInteractiveServerComponents();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddLocalization();
        builder.Services.AddMemoryCache();

        builder.AddHostedService();
        builder.AddGeneral();
        builder.Services.AddScoped<NavigationState>();
        builder.Services.AddScoped<BrowserTimeProvider>();
        // One renderer for the whole application: it carries the pipeline and the cache of
        // the texts it already rendered, and neither belongs to a single circuit.
        builder.Services.AddSingleton<MarkdownService>();
        // Written once at start and read from memory by every crawler that asks for it.
        builder.Services.AddSingleton<SitemapService>();

        builder.InjectCore();
        builder.AddAuthentication(builder.Configuration);
        builder.AddForwardedHeaders(builder.Configuration);
        builder.AddRateLimiting(builder.Configuration);

        builder.AddDatabase(connectionString);
        builder.AddMediator();

        var app = builder.Build();

        // First in the pipeline on purpose: everything downstream that reads the caller address or the
        // request scheme (session metadata checks, sign-in lockout, cookie policy) must see the client,
        // not the proxy.
        app.UseForwardedHeaders();

        // Directly after UseForwardedHeaders, so the limiter partitions on the caller address. It also
        // runs ahead of UseStatusCodePagesWithReExecute, so a rejection keeps the answer of the limiter
        // instead of the page of the status code.
        app.UseRateLimiter();

        // WebApplication adds UseAuthentication and UseAuthorization ahead of every middleware of
        // this file, when this file adds neither one. CookieEvent then reads the address of the
        // proxy, because UseForwardedHeaders has not run yet. The session metadata check fails, and
        // the user loses the session at once. These two calls keep the authentication behind
        // UseForwardedHeaders.
        app.UseAuthentication();
        app.UseAuthorization();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseMiddleware<LocalizationMiddleware>();
        app.UseStatusCodePagesWithReExecute(Frontend.Pages.Systems.Error404Page.Url, createScopeForStatusCodePages: true);
        app.AddEndpoints();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        // A crawler looks for the sitemap at the root of the site and will not go hunting under
        // /api/public, so this one route is mapped here instead of through the endpoint registry.
        app.MapGet("/sitemap.xml", (SitemapService sitemap) => sitemap.Xml.Length == 0
            ? Results.NotFound()
            : Results.Content(sitemap.Xml, "application/xml", Encoding.UTF8));

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStopping.Register(() =>
        {
            Log.Warning("[-] Ctrl+C detected! Performing graceful cleanup operations...");
        });

        try
        {
            MigrationRunner.Up(connectionString);

            Log.Warning("Application: Starting");
            AddAllApiKeysToTheCache(app);
            BuildTheSitemap(app);
            app.Run();

            return ExitSuccess;
        }
        catch (Exception ex)
        {
            // A non-zero exit code is what tells Docker/Kubernetes the container failed
            // and has to be restarted.
            Log.Fatal(ex, "Application start-up failed");

            return ExitFailure;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Writes the sitemap once, out of the works the database carries at this moment. It is the
    /// whole of the public site: the front page and one address per work. Everything else either
    /// sits under the admin segment or is a door rather than a page.
    /// </summary>
    private static void BuildTheSitemap(WebApplication app)
    {
        var origin = app.Configuration[Globals.Settings.ApplicationHost];

        if (string.IsNullOrWhiteSpace(origin))
        {
            // Without an address of its own the site cannot write the absolute urls a sitemap is
            // made of, so it serves none rather than a document full of relative ones.
            Log.Warning("Sitemap: {Setting} is not set, so no sitemap is served", Globals.Settings.ApplicationHost);
            return;
        }

        var factory = app.Services.GetRequiredService<IDbContextFactory<DataContextBusiness>>();

        using (var context = factory.CreateDbContext())
        {
            // A work nobody has written a word of has no page to point at: ProjectReadPage answers
            // 404 for one, and a sitemap that lists 404s is worse than a sitemap that lists less.
            var works = context.Projects
                .Where(x => x.Translations.Count > 0)
                .OrderByDescending(x => x.PublishedAt)
                .Select(x => new { x.Id, x.UpdatedAt })
                .AsNoTracking()
                .ToList();

            var entries = new List<SitemapService.Entry>(works.Count + 1)
            {
                // The front page carries the cards, so it moved when the newest of them moved.
                new(Frontend.Pages.Home.Url, works.Count == 0
                    ? DateTimeOffset.UtcNow
                    : works.Max(x => x.UpdatedAt)),
            };

            entries.AddRange(works.Select(x => new SitemapService.Entry(
                $"{Frontend.Pages.Projects.ProjectReadPage.Url}/{x.Id}",
                x.UpdatedAt)));

            app.Services.GetRequiredService<SitemapService>().Build(origin, entries);

            Log.Warning("Sitemap: {Count} addresses written", entries.Count);
        }
    }

    private static void AddAllApiKeysToTheCache(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContextIdentity>();
            var keys = db.ApiKeys
                .Select(x => new
                {
                    x.AccountId,
                    x.KeyHash,
                    Features = x.Account!.Features.Select(f => f.Id).ToList()
                })
                .AsNoTracking().ToList();

            var cache = scope.ServiceProvider.GetRequiredService<CacheManager>();
            foreach (var key in keys)
            {
                cache.Save(key.KeyHash, key.AccountId, key.Features);
            }
        }
    }
}
