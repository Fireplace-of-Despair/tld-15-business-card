// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using StainlessCore;
using tld15Server.Composition;
using tld15Server.Features.Projects;

namespace tld15Server.Frontend.Pages.Projects;

/// <summary> The list an editor opens a project or an article from, newest first. </summary>
[Authorize(Policy = ProjectSearchFeature.Id)]
public partial class ProjectSearchPage
{
    public const string Url = $"{Globals.Route.Admin}/projects/search";
    public const string UrlParamPage = "{page:int?}";

    [Parameter] public int? Page { get; set; }

    public sealed record Loaded
    {
        public List<ProjectSearchFeature.Item> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
    }

    [PersistentState]
    public Loaded? State { get; set; }

    private bool _canEdit;

    private int MaxPage => State == null || State.TotalCount == 0
        ? 0
        : (State.TotalCount - 1) / Globals.Pagination.PageSize;

    protected override async Task OnInitializedAsync()
    {
        _canEdit = await HasFeatureAsync(ProjectPostFeature.Id);
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
                new ProjectSearchFeature.Query { LanguageId = Language, Page = page }, _cts.Token
            );
        });

        if (result.IncidentCode != null)
        {
            IncidentCode = result.IncidentCode;
            IsLoading = false;
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
