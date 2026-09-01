// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;

namespace tld15Server.Features.Projects;

/// <summary>
/// Removes a project and everything that hangs off it. The translations go with it through the
/// cascade the schema declares, so this deletes one row and the database keeps its own house.
/// </summary>
public sealed class ProjectDeleteFeature : IFeature
{
    public const string Id = "project.delete";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required string Id { get; set; }

        /// <summary> The version the caller read: a stale copy does not get to delete. </summary>
        public required long VersionLocal { get; set; }
    }

    public sealed record Result
    {
        public required string Id { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : ICommandHandler<Command, Result>
    {
        public async ValueTask<Result> Handle(Command cmd, CancellationToken ctn)
        {
            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var project = await contextBusiness
                    .Projects
                    .FirstOrDefaultAsync(x => x.Id == cmd.Id, ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                if (project.VersionLocal != cmd.VersionLocal)
                {
                    throw new IncidentException(IncidentCode.VersionMismatch);
                }

                contextBusiness.Remove(project);

                await contextBusiness.SaveChangesAsync(ctn);

                return new Result { Id = cmd.Id };
            }
        }
    }
}
