// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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

namespace tld15Server.Features.Contents;

/// <summary>
/// Lists the contents an editor can open. The set is short and fixed, so the query names the ids it
/// wants instead of reading the whole table.
/// </summary>
public sealed class ContentSearchFeature : IFeature
{
    public const string Id = "content.search";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string LanguageId { get; set; }
    }

    public sealed record Item
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
    }

    public sealed record Result
    {
        public List<Item> Items { get; set; } = [];
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var contentIds = Globals.Content.LinkEditable;

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var rows = await contextBusiness
                    .Contents
                    .Where(x => contentIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        Titles = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                    })
                    .ToListAsync(ctn);

                return new Result
                {
                    Items = [.. contentIds
                        .Where(id => rows.Exists(x => x.Id == id))
                        .Select(id => new Item
                        {
                            Id = id,
                            Title = rows.First(x => x.Id == id).Titles.GetName(query.LanguageId)
                        })]
                };
            }
        }
    }
}
