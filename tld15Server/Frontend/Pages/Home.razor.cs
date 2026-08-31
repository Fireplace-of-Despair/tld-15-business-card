// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Threading.Tasks;
using StainlessCore;
using tld15Server.Features.Home;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Pages;

public partial class Home
{
    public const string Url = "/";


    public SharedContent? Lore { get; set; } = null;
    public SharedContent? Social { get; set; } = null;
    public SharedContent? Contacts { get; set; } = null;
    public List<SharedProjectPreview> Articles { get; set; } = [];
    public List<SharedProjectPreview> Projects { get; set; } = [];

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
            return;
        }

        Lore = result.Data!.Lore;
        Social = result.Data.Social;
        Contacts = result.Data.Contacts;
        Articles = result.Data.Articles;
        Projects = result.Data.Projects;
    }
}
