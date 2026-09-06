// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;
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

        // A crawler looks for both of these at the root of the site and will not go hunting under
        // /api/public, so these two routes are mapped here instead of through the endpoint registry.
        app.MapGet(Globals.Route.Sitemap, (SitemapService sitemap) => sitemap.Xml.Length == 0
            ? Results.NotFound()
            : Results.Content(sitemap.Xml, "application/xml", Encoding.UTF8));

        // Composed rather than served from the web root, because the sitemap line it carries has to
        // be an absolute address and the host is only known to the configuration.
        app.MapGet(Globals.Route.Robots, (IConfiguration configuration) => Results.Content(
            RobotsService.Build(configuration[Globals.Settings.ApplicationHost]),
            "text/plain",
            Encoding.UTF8));

        // A reader looks for the feed near the root of the site, and IconHelper already draws the
        // feed icon for a "/rss" path, so this one is mapped here rather than under /api/public.
        app.MapGet(Globals.Route.Rss, WriteTheFeed);

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
    /// Answers with the feed. Read per request rather than held from start-up: a reader polls a feed
    /// once and moves on, so an announcement that arrives a deploy late never arrives at all.
    /// </summary>
    /// <remarks>
    /// Without <c>Application:Host</c> there is nothing to answer with. Every address in a feed is
    /// absolute, and a relative one is not something a reader can follow back to the site.
    /// </remarks>
    private static async Task<IResult> WriteTheFeed(
          IMediator mediator
        , IConfiguration configuration
        , IStringLocalizer<Frontend.Localization.Resources> localizer
        , HttpContext context
        , CancellationToken ctn)
    {
        var origin = configuration[Globals.Settings.ApplicationHost];

        if (string.IsNullOrWhiteSpace(origin)) { return Results.NotFound(); }

        var language = Globals.ToStoredLanguage(CultureInfo.CurrentUICulture.Name);

        var works = await mediator.Send(new Features.Rss.RssGetFeature.Query { Language = language }, ctn);

        var channel = new RssService.Channel(
            Title: localizer["Brand"].Value,
            Description: localizer["Brand.Slogan"].Value,
            Language: language,
            Copyright: $"{DateTimeOffset.UtcNow.Year} © {localizer["Brand.Company"].Value}",
            ImagePath: Globals.Image.Logo);

        var entries = works.Entries.ConvertAll(entry => new RssService.Entry(
            Path: $"{Frontend.Pages.Projects.ProjectReadPage.Url}/{entry.Id}",
            Title: entry.Title,
            Description: entry.Subtitle,
            PosterUrl: entry.PosterUrl,
            PublishedAt: entry.PublishedAt));

        context.Response.Headers.CacheControl = "public, max-age=3600";
        context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Cookie);

        return Results.Content(
            RssService.Build(origin, channel, entries),
            Globals.Page.Rss.MediaType,
            Encoding.UTF8);
    }

    /// <summary>
    /// Writes the sitemap once, out of the works the database carries at this moment. It is the
    /// whole of the public site: the two walls of cards and one address per work. Everything else
    /// either sits under the admin segment or is a door rather than a page.
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
                .Select(x => new { x.Id, x.UpdatedAt, x.DivisionId })
                .AsNoTracking()
                .ToList();

            // A wall of cards moved when the newest card on it moved, and the two walls carry
            // different works: the archive division on one, everything else on the other.
            var archived = works.FindAll(x => x.DivisionId == Globals.Divisions.ACD);
            var current = works.FindAll(x => x.DivisionId != Globals.Divisions.ACD);

            var entries = new List<SitemapService.Entry>(works.Count + 2)
            {
                new(Frontend.Pages.Home.Url, current.Count == 0
                    ? DateTimeOffset.UtcNow
                    : current.Max(x => x.UpdatedAt)),
            };

            if (archived.Count > 0)
            {
                entries.Add(new SitemapService.Entry(
                    Frontend.Pages.Archive.ArchivePage.Url,
                    archived.Max(x => x.UpdatedAt)));
            }

            // The press has a wall of its own. Every card on it leads off this site, so the page is
            // the only address there is to offer.
            var mentions = context.Presses
                .AsNoTracking()
                .Select(x => x.UpdatedAt)
                .ToList();

            if (mentions.Count > 0)
            {
                entries.Add(new SitemapService.Entry(Frontend.Pages.Presses.PressPage.Url, mentions.Max()));
            }

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
