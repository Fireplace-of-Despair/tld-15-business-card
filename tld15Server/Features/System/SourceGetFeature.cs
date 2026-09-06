// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
/// The MPL-2.0 license asks for no source from a hosted build. A remote user still cannot read the
/// code that runs, so this endpoint offers the address of it. A modified deployment sets
/// Application:SourceUrl to its own fork. The version string carries the informational version, so
/// a user can find the matching commit.
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
