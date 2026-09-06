// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
