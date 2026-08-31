// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Contents;
using tld15Server.Features.Shared.Business;
using tld15Server.Frontend.Components.Common;

namespace tld15Server.Frontend.Pages.Contents;

[Authorize(Policy = ContentGetFeature.Id)]
public partial class ContentEditPage
{
    public const string Url = "/contents/edit";
    public const string UrlParamId = "{id}";

    /// <summary> What keeps a row of the table from being stored. </summary>
    internal enum RowIssue
    {
        /// <summary> The row is ready to be stored. </summary>
        None = 0,

        /// <summary> The language of the icon is not a code this application stores. </summary>
        Language = 1,

        /// <summary> Another row of the same translation already carries this icon in this language. </summary>
        Duplicate = 2,
    }

    [Parameter] public string Id { get; set; } = string.Empty;

    private ContentGetFeature.Result? _content;

    /// <summary> The rows of the table, held apart from the stored state until the save. </summary>
    private List<SharedContentLink> _links = [];

    /// <summary> A row that cannot be stored blocks the save rather than losing itself quietly. </summary>
    private bool CanSave => !_links.Exists(x => Validate(x) != RowIssue.None);

    protected override async Task OnParametersSetAsync()
    {
        // The list only offers the contents this editor understands. A hand-typed address for any
        // other content reads as a page that is not there.
        if (!Globals.Content.LinkEditable.Contains(Id, StringComparer.Ordinal))
        {
            IncidentCode = StainlessCore.Exceptions.IncidentCode.NotFound;
            IsLoading = false;
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ContentGetFeature.Query { Id = Id, LanguageId = Language }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
            return;
        }

        _content = result.Data!;
        _links = _content.Links;

        IsLoading = false;
    }

    /// <summary>
    /// The same rules the feature applies before it stores a row, so the table says no here instead
    /// of the save saying no later. A key only has to be unique inside its own translation: the
    /// locales keep separate dictionaries.
    /// </summary>
    private RowIssue Validate(SharedContentLink row)
    {
        if (!SharedContentLink.IsLanguageValid(row.Language.Trim()))
        {
            return RowIssue.Language;
        }

        if (_links.Exists(x => !ReferenceEquals(x, row)
            && x.TranslationLanguageId == row.TranslationLanguageId
            && StoredKey(x) == StoredKey(row)))
        {
            return RowIssue.Duplicate;
        }

        return RowIssue.None;
    }

    /// <summary> The key a row lands on, in the form the feature stores it. </summary>
    private static string StoredKey(SharedContentLink row)
    {
        return SharedContentLink.ToKey(
            row.Icon.Trim().ToLowerInvariant(),
            row.Language.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// The icons a row may take. An icon serves as many rows as there are languages, so nothing is
    /// held back; a key this application no longer knows joins the list so opening never drops it.
    /// </summary>
    private static List<string> IconOptions(SharedContentLink row)
    {
        var options = new List<string>(IconHelper.Names);

        if (!string.IsNullOrEmpty(row.Icon) && !options.Contains(row.Icon))
        {
            options.Insert(0, row.Icon);
        }

        return options;
    }

    /// <summary>
    /// Adds a row to the locale the editor is being read in, on an icon that locale does not carry
    /// yet, so two additions in a row never collide.
    /// </summary>
    private void AddLink()
    {
        if (_content == null) { return; }

        var translation = _content.Languages.ContainsKey(Language)
            ? Language
            : _content.Languages.Keys.FirstOrDefault() ?? Language;

        var free = IconHelper.Names.FirstOrDefault(name =>
            !_links.Exists(x => x.TranslationLanguageId == translation && x.Icon == name))
            ?? IconHelper.Names[0];

        _links.Add(new SharedContentLink { TranslationLanguageId = translation, Icon = free });
    }

    private void DeleteLink(SharedContentLink link)
    {
        _links.Remove(link);
    }

    private async Task Save()
    {
        if (_content == null || !CanSave) { return; }

        IncidentCode = null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ContentPostFeature.Command
                {
                    Id = _content.Id,
                    VersionLocal = _content.VersionLocal,
                    Links = _links
                }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            StateHasChanged();
            return;
        }

        // The database owns the versions and the timestamps, so the stored state is read back rather
        // than guessed at.
        await LoadAsync();

        StateHasChanged();
    }
}
