// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Configuration;
using StainlessCore.Common.Helpers;
using StainlessCore.Features;
using tld15Server.Composition;

namespace tld15Server.Features.System;

/// <summary>
/// Answers with the address of the source code that this build runs.
/// </summary>
/// <remarks>
/// The AGPL-3.0-only license, section 13, asks a network service to offer its source to every remote
/// user. A modified deployment sets Application:SourceUrl to its own fork. The version string carries
/// the informational version, so a user can find the matching commit.
/// </remarks>
public sealed class SourceGetFeature : IFeature
{
    public const string Id = "system.source";
    public static string FeatureId => Id;

    public sealed record Result
    {
        public string License { get; init; } = string.Empty;
        public string SourceUrl { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
    }

    public sealed record Query : IQuery<Result> { }

    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, Result>
    {
        public ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var sourceUrl = configuration[Globals.Settings.SourceUrl];
            var assembly = typeof(SourceGetFeature).Assembly;

            return ValueTask.FromResult(new Result
            {
                License = Globals.License.Spdx,
                SourceUrl = sourceUrl!,
                Version = assembly.GetVersion()
            });
        }
    }
}
