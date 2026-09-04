// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Services;

namespace tld15Server.Features.Sessions;

/// <summary>Wipe all sessions for an account, but keeps API-keys alive </summary>
/// <remarks>System feature. Right now users can not wipe sessions</remarks>
public sealed class SessionsWipeFeature : IFeature
{
    public const string Id = "ignore.sessions.wipe";

    public static string FeatureId => Id;

    public sealed record Command : ICommand<bool>
    {
        public required Guid AccountId { get; set; }
    }

    public sealed class Handler(
          CacheManager cacheManager
        , IDbContextFactory<DataContextIdentity> contextIdentityFactory
        ) : ICommandHandler<Command, bool>
    {
        public async ValueTask<bool> Handle(Command command, CancellationToken ctn)
        {
            cacheManager.RemoveSessionByAccount(command.AccountId);
            await RestoreApiKeysAsync(command.AccountId, ctn);

            return true;
        }

        private async Task RestoreApiKeysAsync(Guid accountId, CancellationToken ctn)
        {
            using var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn);

            var keys = await contextIdentity.ApiKeys
                .Where(x => x.AccountId == accountId)
                .Select(x => new
                {
                    x.KeyHash,
                    Features = x.Account!.Features.Select(f => f.Id).ToList()
                })
                .AsNoTracking()
                .ToListAsync(ctn);

            foreach (var key in keys)
            {
                cacheManager.Save(key.KeyHash, accountId, key.Features);
            }
        }
    }
}
