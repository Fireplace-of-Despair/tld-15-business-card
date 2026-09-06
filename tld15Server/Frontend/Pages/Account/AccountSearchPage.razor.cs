// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Accounts;

namespace tld15Server.Frontend.Pages.Account;

[Authorize(Policy = AccountsSearchFeature.Id)]
public partial class AccountSearchPage
{
    public const string Url = $"{Globals.Route.Admin}/accounts/search";
    public const string UrlParamPage = "{page:int?}";

    [Parameter] public int? Page { get; set; }

    public sealed record Loaded
    {
        public List<AccountsSearchFeature.Item> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
    }

    [PersistentState]
    public Loaded? State { get; set; }

    private bool _canManageAccounts;

    private int MaxPage => State == null || State.TotalCount == 0
        ? 0
        : (State.TotalCount - 1) / Globals.Pagination.PageSize;

    protected override async Task OnInitializedAsync()
    {
        _canManageAccounts = await HasFeatureAsync(AccountsPostFeature.Id);
    }

    protected override async Task OnParametersSetAsync()
    {
        var page = Math.Max(0, Page ?? 0);

        if (State?.Page == page)
        {
            IsLoading = false;
            return;
        }

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new AccountsSearchFeature.Query
                {
                    LanguageId = Language,
                    Page = page
                }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            return;
        }

        State = new Loaded
        {
            Items = result.Data!.Items,
            TotalCount = result.Data.TotalCount,
            Page = page
        };

        IsLoading = false;
    }
}
