// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;

namespace tld15Server.Features.Presses;

/// <summary>
/// Removes a mention. Its translations go with it through the cascade the schema declares, so this
/// deletes one row and the database keeps its own house.
/// </summary>
public sealed class PressDeleteFeature : IFeature
{
    public const string Id = "press.delete";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required Guid Id { get; set; }

        /// <summary> The version the caller read: a stale copy does not get to delete. </summary>
        public required long VersionLocal { get; set; }
    }

    public sealed record Result
    {
        public required Guid Id { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : ICommandHandler<Command, Result>
    {
        public async ValueTask<Result> Handle(Command cmd, CancellationToken ctn)
        {
            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var press = await contextBusiness
                    .Presses
                    .FirstOrDefaultAsync(x => x.Id == cmd.Id, ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                if (press.VersionLocal != cmd.VersionLocal)
                {
                    throw new IncidentException(IncidentCode.VersionMismatch);
                }

                contextBusiness.Remove(press);

                await contextBusiness.SaveChangesAsync(ctn);

                return new Result { Id = cmd.Id };
            }
        }
    }
}
