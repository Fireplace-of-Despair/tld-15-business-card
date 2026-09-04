// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using StainlessCore.Common.Helpers;
using tld15Server.Composition;
using tld15Server.Features.Presses;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages.Presses;

/// <summary>
/// The editor of one mention in the press. Where it was published and when sit above the locale
/// tabs; what it was called and what its picture shows sit inside them.
/// </summary>
/// <remarks>
/// Nothing here is stored on this server but the address of it: neither the page mentioned nor the
/// picture beside it is served from here.
/// </remarks>
[Authorize(Policy = PressGetFeature.Id)]
public sealed partial class PressEditPage
{
    public const string Url = $"{Globals.Route.Admin}/press/edit";
    public const string UrlParamId = "{id?}";

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string? Id { get; set; }

    private PressGetFeature.Result? _loaded;

    /// <summary> The mention being edited, held apart from the stored state until the save. </summary>
    private SharedPress _press = new();

    /// <summary>
    /// Tracked apart from the route parameter, so a freshly created mention keeps saving to itself
    /// instead of trying to create itself a second time.
    /// </summary>
    private Guid? _pressId;

    private string _language = string.Empty;

    /// <summary>
    /// The translation of the locale the editor is on. A locale the mention does not carry yet gets
    /// its row here rather than a throwaway one, or the first thing typed into it would be lost.
    /// </summary>
    private SharedPressTranslation Current
    {
        get
        {
            var translation = _press.Translation(_language);

            if (translation == null)
            {
                translation = new SharedPressTranslation { LanguageId = _language };
                _press.Translations.Add(translation);
            }

            return translation;
        }
    }

    /// <summary> The date of the mention, as the date field reads and writes it. </summary>
    private DateTime PublishedOn
    {
        get => _press.PublishedAt.UtcDateTime.Date;
        set => _press.PublishedAt = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    /// <summary> A mention with no address is a card that leads nowhere. </summary>
    private bool IsUrlValid =>
        !string.IsNullOrWhiteSpace(_press.Url)
        && UrlPolicy.IsFollowable(UrlPolicy.Clean(_press.Url))
        && UrlPolicy.SchemeOf(UrlPolicy.Clean(_press.Url)).Length > 0;

    private bool IsPosterValid => UrlPolicy.IsImageSource(UrlPolicy.Clean(_press.PosterUrl));

    /// <summary> A field that cannot be stored blocks the save rather than losing itself quietly. </summary>
    private bool CanSave => IsUrlValid && IsPosterValid;

    protected override async Task OnParametersSetAsync()
    {
        _pressId = Id.ToGuid();

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new PressGetFeature.Query { Id = _pressId }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        _loaded = result.Data!;
        _press = _loaded.Item;

        // Every locale gets a row to type into. The save drops the ones that stayed empty, so an
        // untouched locale never reaches the database.
        foreach (var languageId in _loaded.Languages.Keys)
        {
            if (_press.Translation(languageId) == null)
            {
                _press.Translations.Add(new SharedPressTranslation { LanguageId = languageId });
            }
        }

        // A save reads the mention back, and the tab the editor was on has to survive that.
        if (!_loaded.Languages.ContainsKey(_language))
        {
            _language = _loaded.Languages.ContainsKey(Language)
                ? Language
                : _loaded.Languages.Keys.FirstOrDefault() ?? Language;
        }

        IsLoading = false;
    }

    /// <summary> Whether a locale carries anything, which is what marks its tab. </summary>
    private bool HasTranslation(string languageId)
    {
        var translation = _press.Translation(languageId);

        return translation != null && !translation.IsEmpty;
    }

    private async Task Save()
    {
        if (_loaded == null || !CanSave) { return; }

        IncidentCode = null;

        var created = _pressId == null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new PressPostFeature.Command
                {
                    Press = _press,
                    VersionLocal = _loaded.VersionLocal,
                }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        _pressId = result.Data!.Id;

        // A mention that was just created lives at an address of its own from now on, and the
        // address bar has to say so: a reload of the old one would create it a second time.
        if (created)
        {
            Navigation.NavigateTo($"{Url}/{_pressId}");
            return;
        }

        // The database owns the versions and the timestamps, so the stored state is read back
        // rather than guessed at.
        await LoadAsync();

        StateHasChanged();
    }

    private async Task Delete()
    {
        if (_loaded == null || _pressId == null) { return; }

        if (!await ConfirmAsync(Localizer["Press.ConfirmDelete"].Value)) { return; }

        IncidentCode = null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new PressDeleteFeature.Command
                {
                    Id = _pressId.Value,
                    VersionLocal = _loaded.VersionLocal,
                }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        Navigation.NavigateTo(PressSearchPage.Url);
    }
}
