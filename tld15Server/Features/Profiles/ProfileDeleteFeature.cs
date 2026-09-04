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
using tld15Server.Services;

namespace tld15Server.Features.Profiles;

public sealed class ProfileDeleteFeature : IFeature
{
    public const string Id = "profile.delete";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<bool>
    {
        public required Guid AccountId { get; set; }
    }

    public sealed class Handler(
          IDbContextFactory<DataContextIdentity> contextIdentityFactory
        , CacheManager cacheManager
        ) : ICommandHandler<Command, bool>
    {
        public async ValueTask<bool> Handle(Command command, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var result = await contextIdentity.Accounts
                    .Where(x => x.Id == command.AccountId)
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                contextIdentity.Accounts.Remove(result);
                cacheManager.RemoveSessionByAccount(result.Id);
                await contextIdentity.SaveChangesAsync(ctn);
            }

            return true;
        }
    }
}
