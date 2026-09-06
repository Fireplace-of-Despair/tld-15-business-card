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
using tld15Server.Features.Sessions;
using tld15Server.Features.Shared.System;

namespace tld15Server.Frontend.Pages.Sessions;

[Authorize(Policy = SessionsSearchFeature.Id)]
public partial class SessionSearchPage
{
    public const string Url = $"{Globals.Route.Admin}/sessions/search";
    public const string UrlParamPage = "{page:int?}";

    [Parameter] public int? Page { get; set; }

    public sealed record Loaded
    {
        public List<SharedSession> Sessions { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
    }

    [PersistentState]
    public Loaded? State { get; set; }

    private bool _canDeleteSession;
    private bool _canOpenAccount;
    private readonly HashSet<Guid> _expanded = [];

    private int MaxPage => State == null || State.TotalCount == 0
        ? 0
        : (State.TotalCount - 1) / Globals.Pagination.PageSize;

    protected override async Task OnInitializedAsync()
    {
        _canDeleteSession = await HasFeatureAsync(SessionsDeleteFeature.Id);
        _canOpenAccount = await HasFeatureAsync(Features.Accounts.AccountsPostFeature.Id);
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
            var sessions = await Mediator.Send
            (
                new SessionsSearchFeature.Query { Page = page },
                _cts.Token
            );

            return sessions;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
        }
        if (result.IncidentCode == null)
        {
            State = new Loaded
            {
                Sessions = result.Data!.Sessions,
                TotalCount = result.Data.TotalCount,
                Page = page
            };
        }

        IsLoading = false;
    }

    private void ToggleDetails(Guid id)
    {
        if (!_expanded.Remove(id))
        {
            _expanded.Add(id);
        }
    }

    private async Task ConfirmDelete(Guid id)
    {
        var confirmed = await ConfirmAsync(Localizer["Sessions.ConfirmDelete"].Value);
        if (!confirmed) { return; }

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new SessionsDeleteFeature.Command { Id = id }
                , _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        State?.Sessions.RemoveAll(x => x.Id == id);
        _expanded.Remove(id);
        StateHasChanged();
    }
}
