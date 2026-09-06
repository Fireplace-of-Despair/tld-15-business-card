// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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

    public sealed class Handler(
          IDbContextFactory<DataContextBusiness> contextBusinessFactory
        , IDbContextFactory<DataContextReference> contextReferenceFactory
        ) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var language = query.Language;

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var rows = await contextBusiness
                    .Projects
                    .Where(x => x.DivisionId == Globals.Divisions.ACD)
                    .SelectCards(language, Globals.LanguageFallback)
                    .ToListAsync(ctn);

                // The names of the divisions are a second, short read rather than a second
                // collection in the projection above: joined into one result set they would
                // multiply the rows of the wall instead of adding to them.
                var divisions = await SharedProjectQuery.DivisionNames(contextReferenceFactory, language, ctn);

                var cards = SharedProjectQuery.Split(rows, divisions, language);

                return new Result
                {
                    Articles = cards.Articles,
                    Projects = cards.Projects,
                };
            }
        }
    }
}
