// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StainlessCore;
using tld15Server.Features.Contents;

namespace tld15Server.Frontend.Pages.Contents;

[Authorize(Policy = ContentSearchFeature.Id)]
public partial class ContentSearchPage
{
    public const string Url = "/contents";

    private List<ContentSearchFeature.Item> _items = [];

    protected override async Task OnInitializedAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ContentSearchFeature.Query { LanguageId = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        _items = result.Data!.Items;

        IsLoading = false;
    }
}
