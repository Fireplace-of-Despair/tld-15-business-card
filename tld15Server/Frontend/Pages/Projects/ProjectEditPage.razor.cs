// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Projects;
using tld15Server.Features.Shared.Business;
using tld15Server.Frontend.Components.Common;
using tld15Server.Services;

namespace tld15Server.Frontend.Pages.Projects;

/// <summary>
/// The editor of a project or an article. What the work is and when it was published sits above the
/// locale tabs; the title, the line under it, the words of the poster and the body sit inside them.
/// </summary>
/// <remarks>
/// A poster is an address this server stores, never a file it receives. Nothing is uploaded here.
/// </remarks>
[Authorize(Policy = ProjectGetFeature.Id)]
public sealed partial class ProjectEditPage
{
    public const string Url = $"{Globals.Route.Admin}/projects/edit";
    public const string UrlParamId = "{id?}";

    [GeneratedRegex(Globals.Project.IdPattern)]
    private static partial Regex IdShape();

    /// <summary> What keeps a row of the table of links from being stored. </summary>
    internal enum RowIssue
    {
        /// <summary> The row is ready to be stored. </summary>
        None = 0,

        /// <summary> The language of the icon is not a code this application stores. </summary>
        Language = 1,

        /// <summary> Another row already carries this icon in this language. </summary>
        Duplicate = 2,
    }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string? Id { get; set; }

    private ProjectGetFeature.Result? _loaded;

    /// <summary> The work being edited, held apart from the stored state until the save. </summary>
    private SharedProject _project = new();

    /// <summary>
    /// Tracked apart from the route parameter, so a freshly created work keeps saving to itself
    /// instead of trying to create itself a second time.
    /// </summary>
    private string? _projectId;

    private string _language = string.Empty;

    /// <summary>
    /// The translation of the locale the editor is on. A locale the work does not carry yet gets its
    /// row here rather than a throwaway one, or the first thing typed into it would be lost.
    /// </summary>
    private SharedProjectTranslation Current
    {
        get
        {
            var translation = _project.Translation(_language);

            if (translation == null)
            {
                translation = new SharedProjectTranslation { LanguageId = _language };
                _project.Translations.Add(translation);
            }

            return translation;
        }
    }

    /// <summary> The publication date, as the date field reads and writes it. </summary>
    private DateTime PublishedOn
    {
        get => _project.PublishedAt.UtcDateTime.Date;
        set => _project.PublishedAt = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private bool IsIdValid =>
        _project.Id.Length > 0
        && _project.Id.Length <= Globals.Project.IdMaxLength
        && IdShape().IsMatch(_project.Id)
        && !Globals.Project.IdReserved.Contains(_project.Id, StringComparer.Ordinal);

    private bool IsPosterValid => UrlPolicy.IsImageSource(UrlPolicy.Clean(_project.PosterUrl));

    /// <summary> A field that cannot be stored blocks the save rather than losing itself quietly. </summary>
    private bool CanSave =>
        IsIdValid
        && IsPosterValid
        && !_project.Links.Exists(x => Validate(x) != RowIssue.None);

    protected override async Task OnParametersSetAsync()
    {
        _projectId = string.IsNullOrWhiteSpace(Id) ? null : Id;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProjectGetFeature.Query { Id = _projectId, LanguageId = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        _loaded = result.Data!;
        _project = _loaded.Item;

        // Every locale gets a row to type into. The save drops the ones that stayed empty, so an
        // untouched locale never reaches the database.
        foreach (var languageId in _loaded.Languages.Keys)
        {
            if (_project.Translation(languageId) == null)
            {
                _project.Translations.Add(new SharedProjectTranslation { LanguageId = languageId });
            }
        }

        // A save reads the work back, and the tab the editor was on has to survive that.
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
        var translation = _project.Translation(languageId);

        return translation != null && !translation.IsEmpty;
    }

    /// <summary>
    /// The same rules the feature applies before it stores a row, so the table says no here instead
    /// of the save saying no later. A project keeps one set of links, so a key has to be unique
    /// across the whole table rather than inside a locale.
    /// </summary>
    private RowIssue Validate(SharedLink row)
    {
        if (!SharedLink.IsLanguageValid(row.Language.Trim()))
        {
            return RowIssue.Language;
        }

        if (_project.Links.Exists(x => !ReferenceEquals(x, row) && StoredKey(x) == StoredKey(row)))
        {
            return RowIssue.Duplicate;
        }

        return RowIssue.None;
    }

    /// <summary> The key a row lands on, in the form the feature stores it. </summary>
    private static string StoredKey(SharedLink row)
    {
        return SharedLink.ToKey(row.Icon.Trim().ToLowerInvariant(), row.Language.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// The icons a row may take. An icon serves as many rows as there are languages, so nothing is
    /// held back; a key this application no longer knows joins the list so opening never drops it.
    /// </summary>
    private static List<string> IconOptions(SharedLink row)
    {
        var options = new List<string>(IconHelper.Names);

        if (!string.IsNullOrEmpty(row.Icon) && !options.Contains(row.Icon))
        {
            options.Insert(0, row.Icon);
        }

        return options;
    }

    /// <summary> Adds a row on an icon the work does not carry yet, so two additions never collide. </summary>
    private void AddLink()
    {
        var free = IconHelper.Names.FirstOrDefault(name => !_project.Links.Exists(x => x.Icon == name))
            ?? IconHelper.Names[0];

        _project.Links.Add(new SharedLink { Icon = free });
    }

    private void DeleteLink(SharedLink link)
    {
        _project.Links.Remove(link);
    }

    private async Task Save()
    {
        if (_loaded == null || !CanSave) { return; }

        IncidentCode = null;

        var created = _projectId == null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProjectPostFeature.Command
                {
                    Project = _project,
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

        _projectId = result.Data!.Id;

        // A work that was just created lives at an address of its own from now on, and the address
        // bar has to say so: a reload of the old one would try to create it a second time.
        if (created)
        {
            Navigation.NavigateTo($"{Url}/{_projectId}");
            return;
        }

        // The database owns the versions and the timestamps, so the stored state is read back rather
        // than guessed at.
        await LoadAsync();

        StateHasChanged();
    }

    private async Task Delete()
    {
        if (_loaded == null || _projectId == null) { return; }

        if (!await ConfirmAsync(Localizer["Project.ConfirmDelete"].Value)) { return; }

        IncidentCode = null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ProjectDeleteFeature.Command
                {
                    Id = _projectId,
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

        Navigation.NavigateTo(ProjectSearchPage.Url);
    }
}
