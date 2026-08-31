// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using StainlessCore.Service;
using tld15Server.Features.Profiles;
using tld15Server.Features.Sessions;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Frontend.Pages.Profiles;

[Authorize(Policy = ProfileGetFeature.Id)]
public partial class ProfileReadPage
{
    public const string Url = "/profile";

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    private SharedAccount sharedAccount { get; set; } = new SharedAccount();


    private string? currentPassword { get; set; }

    // Saving wipes this account's sessions, after which the session lookup no longer resolves an id,
    // so it is captured while the session is still alive.
    private Guid? _accountId;

    protected override async Task OnInitializedAsync()
    {
        _accountId = await GetAccountIdAsync();

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProfileGetFeature.Query
                {
                    AccountId = _accountId!.Value,
                    Language = Language
                }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
        }

        if (result.IncidentCode == null)
        {
            sharedAccount = result.Data!.Item;
        }

        IsLoading = false;
    }

    private async Task ConfirmDelete()
    {
        var confirmed = await ConfirmAsync(Localizer["Account.ConfirmDelete"].Value);
        if (!confirmed) { return; }

        var result = await Execute.Run(async () =>
        {
            var delete = await Mediator.Send
            (
                new ProfileDeleteFeature.Command { AccountId = _accountId!.Value }, _cts.Token
            );
            await Mediator.Send
            (
                new SessionsWipeFeature.Command { AccountId = _accountId!.Value }, _cts.Token
            );

            return delete;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        _navigationManager.NavigateTo($"/", true);
    }

    private async Task Save()
    {
        var result = await Execute.Run(async () =>
        {
            // Save first, wipe after. The other order charged a rejected password the session as well:
            // the wipe had already run, so a typo signed the owner out and changed nothing.
            var saved = await Mediator.Send
            (
                new ProfilePutFeature.Command
                {
                    AccountId = _accountId!.Value,
                    Account = sharedAccount,
                    CurrentPassword = currentPassword
                }, _cts.Token
            );

            await Mediator.Send
                (
                    new SessionsWipeFeature.Command { AccountId = _accountId!.Value }, _cts.Token
                );

            return saved;
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        currentPassword = null;

        // Reloading replaces the pending keys with their stored form, which is what hides the plain
        // text: it is never sent back after the save.
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var reloaded = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProfileGetFeature.Query
                {
                    AccountId = _accountId!.Value,
                    Language = Language
                }, _cts.Token
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
