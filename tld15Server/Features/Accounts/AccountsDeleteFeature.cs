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

namespace tld15Server.Features.Accounts;

public sealed class AccountsDeleteFeature : IFeature
{
    public const string Id = "accounts.delete";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<bool>
    {
        public required Guid Id { get; set; }
    }

    public sealed class Handler(
          IDbContextFactory<DataContextIdentity> contextIdentityFactory
        ) : ICommandHandler<Command, bool>
    {
        public async ValueTask<bool> Handle(Command command, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var result = await contextIdentity.Accounts
                    .Where(x => x.Id == command.Id)
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                contextIdentity.Accounts.Remove(result);
                await contextIdentity.SaveChangesAsync(ctn);
            }

            return true;
        }
    }
}
