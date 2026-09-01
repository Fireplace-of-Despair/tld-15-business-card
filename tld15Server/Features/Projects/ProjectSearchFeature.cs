// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Common.Helpers;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;

namespace tld15Server.Features.Projects;

/// <summary>
/// Lists the projects and the articles an editor can open, newest first. The list grows with every
/// work published, so it reads one page at a time.
/// </summary>
public sealed class ProjectSearchFeature : IFeature
{
    public const string Id = "project.search";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string LanguageId { get; set; }
        public int Page { get; set; }
    }

    public sealed record Item
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string ProjectTypeId { get; set; }
        public required string DivisionId { get; set; }
        public required DateTimeOffset PublishedAt { get; set; }
    }

    public sealed record Result
    {
        public List<Item> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var page = Math.Max(0, query.Page);

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var total = await contextBusiness.Projects.CountAsync(ctn);

                // The date decides the order and the id breaks a tie, so a row cannot appear on two
                // pages when two works carry the same date.
                var rows = await contextBusiness
                    .Projects
                    .OrderByDescending(x => x.PublishedAt)
                    .ThenBy(x => x.Id)
                    .Skip(page * Globals.Pagination.PageSize)
                    .Take(Globals.Pagination.PageSize)
                    .Select(x => new
                    {
                        x.Id,
                        x.ProjectTypeId,
                        x.DivisionId,
                        x.PublishedAt,
                        Titles = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Title))
                    })
                    .AsNoTracking()
                    .ToListAsync(ctn);

                return new Result
                {
                    TotalCount = total,
                    Items = rows.ConvertAll(x => new Item
                    {
                        Id = x.Id,
                        Title = x.Titles.GetName(query.LanguageId),
                        ProjectTypeId = x.ProjectTypeId,
                        DivisionId = x.DivisionId,
                        PublishedAt = x.PublishedAt,
                    })
                };
            }
        }
    }
}
