// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Home;
using tld15Server.Features.Shared.Business;
using tld15Server.Frontend.Navigations;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages;

/// <summary>
/// The front of the site. It carries no interactivity of its own, so it renders statically: the
/// first response already holds every block, which is what a reader waits for and what a crawler
/// indexes without running a line of script.
/// </summary>
public partial class Home
{
    public const string Url = "/";

    /// <summary> How long a description a search engine will show before it cuts one itself. </summary>
    private const int DescriptionLength = 160;

    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private MarkdownService Markdown { get; set; } = default!;

    public SharedContent? Lore { get; set; }
    public SharedContent? Social { get; set; }
    public SharedContent? Contacts { get; set; }
    public List<SharedCardPreview> Articles { get; set; } = [];
    public List<SharedCardPreview> Projects { get; set; } = [];

    /// <summary> The one address of the front page, whatever address the reader arrived on. </summary>
    private string _canonical = string.Empty;

    /// <summary> The address of the mark, for a share card that has no work to show a poster of. </summary>
    private string _logo = string.Empty;

    /// <summary>
    /// What the page says about itself. It is the opening of the lore rather than a line written
    /// beside it: a description kept by hand goes stale the first time the text is edited.
    /// </summary>
    private string _description = string.Empty;

    /// <summary> The site as schema.org describes it, wrapped in the element that carries it. </summary>
    private MarkupString _structuredData;

    /// <summary> The blocks this page renders, for the row of links that jumps between them. </summary>
    private List<LocalNavigation.Section> _sections = [];

    protected override async Task OnInitializedAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new HomeGetFeature.Query { Language = Language },
                _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        Lore = result.Data!.Lore;
        Social = result.Data.Social;
        Contacts = result.Data.Contacts;
        Articles = result.Data.Articles;
        Projects = result.Data.Projects;

        BuildAddresses();
        BuildSections();
        BuildStructuredData();

        IsLoading = false;
    }

    /// <summary>
    /// The blocks the page actually renders, in the order it renders them. A link to a heading that
    /// is not on the page scrolls a reader nowhere, so an empty block does not get one.
    /// </summary>
    private void BuildSections()
    {
        _sections = [];

        if (Lore != null && !string.IsNullOrEmpty(Lore.Markdown))
        {
            _sections.Add(new LocalNavigation.Section(Lore.Id, Lore.Title));
        }

        if (Articles.Count > 0)
        {
            _sections.Add(new LocalNavigation.Section(Globals.Anchor.Articles, Localizer["Articles"].Value));
        }

        if (Projects.Count > 0)
        {
            _sections.Add(new LocalNavigation.Section(Globals.Anchor.Projects, Localizer["Projects"].Value));
        }

        if (Social != null && !string.IsNullOrEmpty(Social.LinksJson))
        {
            _sections.Add(new LocalNavigation.Section(Social.Id, Social.Title));
        }

        if (Contacts != null && !string.IsNullOrEmpty(Contacts.LinksJson))
        {
            _sections.Add(new LocalNavigation.Section(Contacts.Id, Contacts.Title));
        }
    }

    /// <summary>
    /// The address of the site and of its mark. The host of the configuration wins over the host of
    /// the request: a site reachable under two names still has a single address a crawler keeps.
    /// </summary>
    private void BuildAddresses()
    {
        var configured = Configuration[Globals.Settings.ApplicationHost];

        var origin = (string.IsNullOrWhiteSpace(configured) ? Navigation.BaseUri : configured).TrimEnd('/');

        _canonical = $"{origin}/";
        _logo = $"{origin}/images/logo.png";

        _description = Markdown.ToSummary(Lore?.Markdown, DescriptionLength);

        if (_description.Length == 0)
        {
            _description = Localizer["Brand.Slogan"].Value;
        }
    }

    /// <summary>
    /// Who this site belongs to and where else that same body can be found. The profiles come from
    /// the social block, so the answer is the one the page already shows rather than a second list
    /// kept in a file nobody opens.
    /// </summary>
    private void BuildStructuredData()
    {
        var organisation = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = Localizer["Brand.Company"].Value,
            ["url"] = _canonical,
            ["logo"] = _logo,
            ["description"] = _description,
        };

        var profiles = Profiles();

        if (profiles.Count > 0) { organisation["sameAs"] = profiles; }

        var site = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = Localizer["Brand"].Value,
            ["url"] = _canonical,
            ["inLanguage"] = Language,
        };

        // The default encoder escapes the characters that would end the element early, so nothing an
        // editor typed can break out of the tag the json is written into.
        var json = JsonSerializer.Serialize(new[] { organisation, site });

        _structuredData = new MarkupString($"<script type=\"application/ld+json\">{json}</script>");
    }

    /// <summary>
    /// The profiles of the social block, addresses only. A mail address is somewhere to write to,
    /// not another place the same body is published, so it does not belong in this list.
    /// </summary>
    private List<string> Profiles()
    {
        var result = new List<string>();

        // A link is stored as the address it opens against the language it speaks, so the address
        // is the key of the row rather than its value.
        foreach (var link in Social?.LinksToDictionary() ?? [])
        {
            var scheme = UrlPolicy.SchemeOf(link.Key);

            if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(link.Key);
            }
        }

        return result;
    }
}
