// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Presses;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages.Presses;

/// <summary>
/// What other people wrote about this body of work, as the same wall of cards the front page draws.
/// Every card leads off this site: the words are somebody else's and are read where they were
/// published.
/// </summary>
public sealed partial class PressPage
{
    public const string Url = "/press";

    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    public List<SharedCardPreview> Items { get; set; } = [];

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
        _description = Localizer["Press.Description"].Value;
        _twitterSite = Configuration[Globals.Settings.TwitterSite] ?? string.Empty;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new PressListFeature.Query { Language = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        Items = result.Data!.Items;

        // After the rows, not before them: the host is read off the first poster the wall will draw.
        _preconnect = UrlPolicy.PreconnectFor(origin, Items.Count > 0 ? Items[0].PosterUrl : null);

        IsLoading = false;
    }
}
