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

namespace tld15Server.Frontend.Pages.Contents;

/// <summary>
/// The editor of a content whose body is prose: one tab per locale, and inside it the markdown
/// source next to its preview.
/// </summary>
[Authorize(Policy = ContentGetFeature.Id)]
public partial class ContentTextEditPage
{
    public const string Url = "/contents/text";
    public const string UrlParamId = "{id}";

    [Parameter] public string Id { get; set; } = string.Empty;

    private ContentGetFeature.Result? _content;

    /// <summary> The body of every locale, held apart from the stored state until the save. </summary>
    private Dictionary<string, string> _texts = [];

    private string _language = string.Empty;

    /// <summary> The body of the locale the editor is on. </summary>
    private string Text
    {
        get => _texts.TryGetValue(_language, out var text) ? text : string.Empty;
        set => _texts[_language] = value;
    }

    protected override async Task OnParametersSetAsync()
    {
        // The list only offers the contents this editor understands. A hand-typed address for any
        // other content reads as a page that is not there.
        if (!Globals.Content.TextEditable.Contains(Id, StringComparer.Ordinal))
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
        _texts = new Dictionary<string, string>(_content.Markdown, StringComparer.Ordinal);

        // A save reads the content back, and the tab the editor was working on has to survive that.
        // Only a locale the content does not carry moves the selection.
        if (!_content.Languages.ContainsKey(_language))
        {
            _language = _content.Languages.ContainsKey(Language)
                ? Language
                : _content.Languages.Keys.FirstOrDefault() ?? Language;
        }

        IsLoading = false;
    }

    /// <summary> Whether a locale carries a body at all. </summary>
    private bool HasText(string languageId)
    {
        return _texts.TryGetValue(languageId, out var text) && !string.IsNullOrWhiteSpace(text);
    }

    private async Task Save()
    {
        if (_content == null) { return; }

        IncidentCode = null;

        var result = await Execute.Run(async () =>
        {
            return await Mediator.Send
            (
                new ContentPostFeature.Command
                {
                    Id = _content.Id,
                    VersionLocal = _content.VersionLocal,
                    Markdown = _texts.ToDictionary(x => x.Key, x => (string?)x.Value, StringComparer.Ordinal),
                    // The links of the content belong to the other editor. They travel back exactly
                    // as they were read: the command replaces the whole content, not one side of it.
                    Links = _content.Links,
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
