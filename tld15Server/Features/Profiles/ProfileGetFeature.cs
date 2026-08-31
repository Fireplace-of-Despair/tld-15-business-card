// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Features.Profiles;

public class ProfileGetFeature : IFeature
{
    public const string Id = "profile.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required Guid AccountId { get; set; }
        public required string Language { get; set; }
    }

    public sealed record Result
    {
        public required SharedAccount Item { get; set; }
    }

    public sealed class Handler(
      IDbContextFactory<DataContextIdentity> contextIdentityFactory
        ) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var account = await LoadAccount(query, ctn);

            return new Result
            {
                Item = account,
            };
        }

        private async Task<SharedAccount> LoadAccount(Query query, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var result = await contextIdentity.Accounts
                    .AsNoTracking()
                    .Where(x => x.Id == query.AccountId)
                    .Select(x => new
                    {
                        x.Id,
                        Features = x.Features.Select(x => x.Id).ToList(),
                        x.Login,
                        x.AccountTypeId,
                        x.AccountStatusId,
                        // The hash is never sent to the UI: there is nothing a user could do with it.
                        ApiKeys = x.ApiKeys.Select(key => new { key.Id, key.CreatedAt, key.LastUsedAt }),
                        x.CreatedAt,
                        x.UpdatedAt,
                        x.VersionLocal
                    })
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);


                return new SharedAccount
                {
                    Id = result.Id,
                    Features = result.Features,
                    Login = result.Login,
                    TypeId = result.AccountTypeId,
                    Password = null,
                    StatusId = result.AccountStatusId,
                    ApiKeys = [.. result.ApiKeys.Select(key => new SharedApiKey
                    {
                        Id = key.Id,
                        CreatedAt = key.CreatedAt,
                        LastUsedAt = key.LastUsedAt
                    })],
                    CreatedAt = result.CreatedAt,
                    UpdatedAt = result.UpdatedAt,
                    VersionLocal = result.VersionLocal
                };
            }
        }
    }
}
