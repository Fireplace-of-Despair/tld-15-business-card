// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Features;
using StainlessInfrastructure;

namespace tld15Server.Features.System;

public sealed class PingGetFeature : IFeature
{
    public const string Id = "system.ping";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<string> { }

    public sealed class Handler(IDbContextFactory<DataContextIdentity> contextIdentityFactory) : IQueryHandler<Query, string>
    {
        private const string ResponsePong = "pong";
        private const string ResponseWrong = "wrong";

        public async ValueTask<string> Handle(Query query, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                return await contextIdentity.Database.CanConnectAsync(ctn)
                ? ResponsePong
                : ResponseWrong;
            }
        }
    }
}
