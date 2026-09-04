// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using StainlessCore.Features;
using tld15Server.Services;

namespace tld15Server.Features.Identity;

public sealed class IdentityDeleteFeature : IFeature
{
    public const string Id = "identity.delete";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<bool>
    {
        public required Guid SessionId { get; set; }
    }

    public sealed class Handler(CacheManager cacheManager) : IQueryHandler<Query, bool>
    {
        public async ValueTask<bool> Handle(Query query, CancellationToken ctn)
        {
            await Task.Delay(0, ctn);

            cacheManager.RemoveSessionById(query.SessionId);
            return true;
        }
    }
}
