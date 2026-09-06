// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Contents;

namespace tld15Server.Frontend.Pages.Contents;

[Authorize(Policy = ContentSearchFeature.Id)]
public partial class ContentSearchPage
{
    public const string Url = $"{Globals.Route.Admin}/contents";

    private List<ContentSearchFeature.Item> _items = [];

    /// <summary>
    /// The editor a content opens in. A content carries either a table of links or a body of prose,
    /// and this list is the one place that knows which page each of them belongs to.
    /// </summary>
    private static string EditUrl(string id)
    {
        return Globals.Content.TextEditable.Contains(id, StringComparer.Ordinal)
            ? $"{ContentTextEditPage.Url}/{id}"
            : $"{ContentLinkEditPage.Url}/{id}";
    }

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
