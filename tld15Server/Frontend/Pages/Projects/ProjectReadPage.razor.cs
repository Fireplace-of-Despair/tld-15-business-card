// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Projects;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Pages.Projects;

/// <summary>
/// The page a reader lands on. It carries no interactivity of its own, so it renders statically:
/// the first response already holds the whole text, which is both what a crawler indexes and what
/// a reader waits for.
/// </summary>
public sealed partial class ProjectReadPage
{
    public const string Url = "/projects";
    public const string UrlParamId = "{id}";

    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private SharedProject? _project;
    private SharedProjectTranslation? _translation;

    private string _divisionName = string.Empty;
    private DateTimeOffset _updatedAt;
    private long _versionLocal;
    private string _canonical = string.Empty;

    private readonly List<string> _otherLocales = [];
    private string _shareCard = string.Empty;
    private bool _shareCardIsPoster;
    private string _twitterSite = string.Empty;
    private MarkupString _structuredData;

    private string Title => _translation?.Title ?? string.Empty;
    private string Subtitle => _translation?.Subtitle ?? string.Empty;

    /// <summary> A date as the cards write it, so one work reads the same everywhere on the site. </summary>
    private static string Date(DateTimeOffset value)
    {
        return value.ToString(Globals.Settings.DateFormat, CultureInfo.InvariantCulture);
    }

    /// <summary> The machine form of a date, which is what a crawler reads out of a time element. </summary>
    private static string Stamp(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string StampIso(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    protected override async Task OnParametersSetAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProjectGetFeature.Query { Id = Id, LanguageId = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            Report(result.IncidentCode.Value);
            return;
        }

        var loaded = result.Data!;

        // A work nobody has written a word of is not a page: answering with an empty article would
        // put a blank entry into the index.
        var translation = Pick(loaded.Item);

        if (translation == null)
        {
            Report(StainlessCore.Exceptions.IncidentCode.NotFound);
            return;
        }

        _project = loaded.Item;
        _translation = translation;
        _updatedAt = loaded.UpdatedAt;
        _versionLocal = loaded.VersionLocal;
        _divisionName = loaded.Divisions.GetValueOrDefault(loaded.Item.DivisionId, loaded.Item.DivisionId);

        BuildAddresses(loaded);
        BuildStructuredData(loaded);

        IsLoading = false;
    }

    /// <summary>
    /// The locale the reader asked for, the locale the site falls back to, or whichever one the work
    /// carries. A reader who arrives in a locale this work does not speak still gets to read it.
    /// </summary>
    private static SharedProjectTranslation? Pick(SharedProject project)
    {
        return project.Translation(Language)
            ?? project.Translation(Globals.LanguageFallback)
            ?? project.Translations.FirstOrDefault();
    }

    /// <summary>
    /// Says out loud that the page is not there. A crawler that reads a friendly message under a 200
    /// files the address away as a real page, and every missing work becomes an entry in the index.
    /// </summary>
    private void Report(StainlessCore.Exceptions.IncidentCode code)
    {
        IncidentCode = code;
        IsLoading = false;

        var context = HttpContextAccessor.HttpContext;

        if (context == null || context.Response.HasStarted) { return; }

        context.Response.StatusCode = code == StainlessCore.Exceptions.IncidentCode.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status500InternalServerError;
    }

    /// <summary>
    /// The one address of the work. The host of the configuration wins over the host of the request:
    /// a site reachable under two names still has a single address a crawler should keep.
    /// </summary>
    /// <remarks>
    /// The locales share that address, because this site picks a locale from a cookie rather than
    /// from the path. There is nothing to point an hreflang at, so the page claims none; the other
    /// locales are named for the share cards and nothing more.
    /// </remarks>
    private void BuildAddresses(ProjectGetFeature.Result loaded)
    {
        var configured = Configuration[Globals.Settings.ApplicationHost];

        var origin = (string.IsNullOrWhiteSpace(configured) ? Navigation.BaseUri : configured).TrimEnd('/');

        _canonical = $"{origin}{Url}/{loaded.Item.Id}";

        _twitterSite = Configuration[Globals.Settings.TwitterSite] ?? string.Empty;

        _shareCardIsPoster = !string.IsNullOrWhiteSpace(loaded.Item.PosterUrl);

        _shareCard = _shareCardIsPoster
            ? Absolute(loaded.Item.PosterUrl)
            : $"{origin}{Globals.Image.ShareCard}";

        _otherLocales.Clear();
        _otherLocales.AddRange(loaded.Item.Translations
            .Select(x => x.LanguageId)
            .Where(x => x != _translation!.LanguageId));
    }

    /// <summary> An address a share card and a crawler can both resolve, whatever the editor typed. </summary>
    private string Absolute(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) { return string.Empty; }

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) { return url; }

        var origin = _canonical[.._canonical.IndexOf(Url, StringComparison.Ordinal)];

        return $"{origin}/{url.TrimStart('/')}";
    }

    /// <summary>
    /// The work as schema.org describes it. A crawler that reads this does not have to guess which
    /// line of the page is the headline and which date is the one the work was published on.
    /// </summary>
    private void BuildStructuredData(ProjectGetFeature.Result loaded)
    {
        var poster = Absolute(loaded.Item.PosterUrl);

        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["@context"] = "https://schema.org",
            ["@type"] = loaded.Item.ProjectTypeId == Globals.ProjectType.Article ? "Article" : "CreativeWork",
            ["headline"] = Title,
            ["datePublished"] = StampIso(loaded.Item.PublishedAt),
            ["dateModified"] = StampIso(loaded.UpdatedAt),
            ["inLanguage"] = _translation!.LanguageId,
            ["version"] = loaded.VersionLocal,
            ["mainEntityOfPage"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["@type"] = "WebPage",
                ["@id"] = _canonical,
            },
            ["author"] = Organisation(),
            ["publisher"] = Organisation(),
        };

        if (!string.IsNullOrWhiteSpace(Subtitle)) { data["description"] = Subtitle; }

        // Only a real poster. A share card has to show something or it renders as a broken frame,
        // but schema.org "image" is read as a picture *of* this work, and the mark of the site is
        // not one. An absent field is honest; a stand-in in this field is not.
        if (!string.IsNullOrWhiteSpace(poster)) { data["image"] = poster; }

        // The default encoder escapes the characters that would end the element early, so nothing an
        // editor typed can break out of the tag the json is written into.
        var json = JsonSerializer.Serialize(data);

        _structuredData = new MarkupString($"<script type=\"application/ld+json\">{json}</script>");
    }

    private Dictionary<string, object> Organisation()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["@type"] = "Organization",
            ["name"] = Localizer["Brand.Company"].Value,
        };
    }
}
