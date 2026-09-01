// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Archive;

/// <summary>
/// The works the front page does not carry: everything filed under the archive division. They are
/// ordinary works and open on the ordinary page — this reads the same columns the front page reads,
/// asking for the other half of the table.
/// </summary>
public sealed class ArchiveGetFeature : IFeature
{
    public const string Id = "archive.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Result
    {
        public List<SharedCardPreview> Articles { get; set; } = [];
        public List<SharedCardPreview> Projects { get; set; } = [];
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var language = query.Language;

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var rows = await contextBusiness
                    .Projects
                    .Where(x => x.DivisionId == Globals.Archive.DivisionId)
                    .SelectCards(language, Globals.LanguageFallback)
                    .ToListAsync(ctn);

                var cards = SharedProjectQuery.Split(rows, language);

                return new Result
                {
                    Articles = cards.Articles,
                    Projects = cards.Projects,
                };
            }
        }
    }
}
