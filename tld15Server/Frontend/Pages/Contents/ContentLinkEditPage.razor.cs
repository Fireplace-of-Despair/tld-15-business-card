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
using tld15Server.Services;

namespace tld15Server.Frontend.Pages.Contents;

[Authorize(Policy = ContentGetFeature.Id)]
public partial class ContentLinkEditPage
{
    public const string Url = $"{Globals.Route.Admin}/contents/links";
    public const string UrlParamId = "{id}";

    /// <summary> What keeps a row of the table from being stored. </summary>
    internal enum RowIssue
    {
        /// <summary> The row is ready to be stored. </summary>
        None = 0,

        /// <summary> The language of the link is not a code this application stores. </summary>
        Language = 1,

        /// <summary> Another row already carries this address. </summary>
        Duplicate = 2,
    }

    [Parameter] public string Id { get; set; } = string.Empty;

    private ContentGetFeature.Result? _content;

    /// <summary> The rows of the table, held apart from the stored state until the save. </summary>
    private List<SharedLink> _links = [];

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
    /// of the save saying no later. A content keeps one set of links, so an address has to be unique
    /// across the whole table.
    /// </summary>
    private RowIssue Validate(SharedLink row)
    {
        if (!SharedLink.IsLanguageValid(row.Language.Trim()))
        {
            return RowIssue.Language;
        }

        var url = StoredUrl(row);

        // A row with nothing typed into it is not a link and the save drops it, so two blank rows
        // are not a collision the editor has to complain about.
        if (url.Length > 0 && _links.Exists(x => !ReferenceEquals(x, row) && StoredUrl(x) == url))
        {
            return RowIssue.Duplicate;
        }

        return RowIssue.None;
    }

    /// <summary> The key a row lands on, which is its address in the form the feature stores it. </summary>
    private static string StoredUrl(SharedLink row)
    {
        return UrlPolicy.Clean(SharedLink.ToStoredUrl(row.Url));
    }

    /// <summary>
    /// Adds an empty row to the table. The icon follows from the address once one is typed into it,
    /// so a fresh row carries nothing at all.
    /// </summary>
    private void AddLink()
    {
        _links.Add(new SharedLink());
    }

    private void DeleteLink(SharedLink link)
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
                    Links = _links,
                    Markdown = _content.Markdown.ToDictionary(x => x.Key, x => (string?)x.Value, StringComparer.Ordinal),
                    PosterUrl = _content.PosterUrl ?? string.Empty,
                    PosterAlt = _content.PosterAlt.ToDictionary(x => x.Key, x => (string?)x.Value, StringComparer.Ordinal)
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
