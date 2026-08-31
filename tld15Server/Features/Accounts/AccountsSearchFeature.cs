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

namespace tld15Server.Features.Accounts;

public sealed class AccountsSearchFeature : IFeature
{
    public const string Id = "accounts.search";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string LanguageId { get; set; }
        public int Page { get; set; }
    }

    public sealed record Item
    {
        public required Guid Id { get; set; }
        public required string Login { get; set; }
        public required string Status { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }

    public sealed record Result
    {
        public List<Item> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    public sealed class Handler(
        IDbContextFactory<DataContextIdentity> contextIdentityFactory
        ) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var page = Math.Max(0, query.Page);

            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var total = await contextIdentity.Accounts.CountAsync(ctn);

                // Login is unique, so the order is stable and a row cannot appear on two pages.
                var result = await contextIdentity.Accounts
                .OrderBy(x => x.Login)
                .Skip(page * Globals.Pagination.PageSize)
                .Take(Globals.Pagination.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Login,
                    Statuses = x.AccountStatus!
                        .Translations
                        .Select(x => new KeyValuePair<string, string>(x.LanguageId, x.Name)),
                    x.CreatedAt
                })
                .AsNoTracking()
                .ToListAsync(ctn);

                return new Result
                {
                    TotalCount = total,
                    Items = result.ConvertAll(x => new Item
                    {
                        Id = x.Id,
                        Login = x.Login,
                        Status = x.Statuses.GetName(query.LanguageId),
                        CreatedAt = x.CreatedAt
                    })
                };
            }
        }
    }
}
