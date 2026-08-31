// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using StainlessCore.Features;
using tld15Server.Composition;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15Server.Features.Sessions;

public sealed class SessionsSearchFeature : IFeature
{
    public const string Id = "sessions.search";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public int Page { get; set; }
    }

    public sealed record Result
    {
        public List<SharedSession> Sessions { get; set; } = [];
        public int TotalCount { get; set; }
    }

    public sealed class Handler(CacheManager cacheManager) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            await Task.Delay(0, ctn);

            var page = Math.Max(0, query.Page);

            // The sessions live in memory, so the page is cut here and not in a database.
            var all = cacheManager.GetAllSessions()
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return new Result
            {
                TotalCount = all.Count,
                Sessions = [.. all
                    .Skip(page * Globals.Pagination.PageSize)
                    .Take(Globals.Pagination.PageSize)]
            };
        }
    }
}
