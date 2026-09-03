// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Archive;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages.Archive;

/// <summary>
/// The works of the archive division, as the same wall of cards the front page draws. A card leads
/// to the ordinary page of the work: an archived work is filed differently, not read differently.
/// </summary>
public sealed partial class ArchivePage
{
    public const string Url = "/archive";

    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    public List<SharedCardPreview> Articles { get; set; } = [];
    public List<SharedCardPreview> Projects { get; set; } = [];

    private string _canonical = string.Empty;
    private string _description = string.Empty;
    private string _shareCard = string.Empty;
    private string _preconnect = string.Empty;
    private string _twitterSite = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var configured = Configuration[Globals.Settings.ApplicationHost];
        var origin = (string.IsNullOrWhiteSpace(configured) ? Navigation.BaseUri : configured).TrimEnd('/');

        _canonical = $"{origin}{Url}";
        _shareCard = $"{origin}{Globals.Image.ShareCard}";
        _description = Localizer["Archive.Description"].Value;
        _twitterSite = Configuration[Globals.Settings.TwitterSite] ?? string.Empty;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ArchiveGetFeature.Query { Language = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        Articles = result.Data!.Articles;
        Projects = result.Data.Projects;

        // After the rows, not before them: the host is read off the first poster the wall will draw.
        _preconnect = UrlPolicy.PreconnectFor(
            origin,
            Articles.Count > 0 ? Articles[0].PosterUrl : null,
            Projects.Count > 0 ? Projects[0].PosterUrl : null);

        IsLoading = false;
    }
}
