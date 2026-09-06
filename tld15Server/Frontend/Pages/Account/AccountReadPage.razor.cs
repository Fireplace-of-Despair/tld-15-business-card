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
using StainlessCore.Common.Helpers;
using StainlessCore.Service;
using tld15Server.Features.Accounts;
using tld15Server.Features.Sessions;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Frontend.Pages.Account;

[Authorize(Policy = AccountsGetFeature.Id)]
public partial class AccountReadPage
{
    public const string Url = $"{Globals.Route.Admin}/accounts";
    public const string UrlParamId = "{id?}";

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    [Parameter] public string? Id { get; set; }

    private SharedAccount sharedAccount { get; set; } = new SharedAccount();

    private Dictionary<string, string> _statuses = [];
    private Dictionary<string, string> _features = [];
    private Dictionary<string, string> _types = [];

    // Tracked separately from the route parameter so a freshly created account keeps saving to itself.
    private Guid? _accountId;

    protected override async Task OnInitializedAsync()
    {
        _accountId = Id.ToGuid();

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new AccountsGetFeature.Query { Id = _accountId, Language = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
        }

        if (result.IncidentCode == null)
        {
            sharedAccount = result.Data!.Item;
            _statuses = result.Data.Statuses;
            _features = result.Data.Features;
            _types = result.Data.Types;
        }

        IsLoading = false;
    }

    private async Task ConfirmDelete()
    {
        var confirmed = await ConfirmAsync(Localizer["Account.ConfirmDelete"].Value);
        if (!confirmed) { return; }

        var result = await Execute.Run(async () =>
        {
            var result = await Mediator.Send
            (
                new AccountsDeleteFeature.Command { Id = _accountId!.Value }, _cts.Token
            );

            if (_accountId != null)
            {
                await Mediator.Send
                (
                    new SessionsWipeFeature.Command { AccountId = _accountId.Value }, _cts.Token
                );
            }
            return result;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        _navigationManager.NavigateTo($"{AccountSearchPage.Url}", true);
    }

    private async Task Save()
    {
        var result = await Execute.Run(async () =>
        {
            var result = await Mediator.Send
            (
                new AccountsPostFeature.Command { Account = sharedAccount }, _cts.Token
            );
            if (_accountId != null)
            {
                await Mediator.Send
                (
                    new SessionsWipeFeature.Command { AccountId = _accountId.Value }, _cts.Token
                );
            }
            return result;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        _accountId = result.Data!.Id;

        // Reloading replaces the pending keys with their stored form, which is what hides the plain
        // text: it is never sent back after the save.
        await ReloadAsync();

        // Keeps the address bar honest for a newly created account, without restarting the circuit.
        _navigationManager.NavigateTo($"{Url}/{_accountId}");
    }

    private async Task ReloadAsync()
    {
        var reloaded = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new AccountsGetFeature.Query { Id = _accountId, Language = Language }, _cts.Token
            );
        });

        if (reloaded.IncidentCode != null)
        {
            IncidentCode = reloaded.IncidentCode;
        }
        else
        {
            sharedAccount = reloaded.Data!.Item;
        }

        StateHasChanged();
    }

    private async Task OnFeatureChanged(ChangeEventArgs e, string featureKey)
    {
        var isChecked = (bool)e.Value!;
        if (isChecked)
        {
            if (!sharedAccount.Features.Contains(featureKey))
            {
                sharedAccount.Features.Add(featureKey);
            }
        }
        else
        {
            sharedAccount.Features.Remove(featureKey);
        }
    }

    // Issued straight away so the value can be shown and copied; it is stored, hashed, on save.
    private void AddKey()
    {
        sharedAccount.ApiKeys.Add(new SharedApiKey { Value = ApiKeyService.Generate() });
    }

    private void DeleteKey(SharedApiKey key)
    {
        sharedAccount.ApiKeys.Remove(key);
    }
}
