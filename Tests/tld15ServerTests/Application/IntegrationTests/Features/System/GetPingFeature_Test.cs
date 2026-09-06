// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessInfrastructure;
using tld15Server.Features.System;
namespace tld15ServerTests.Application.IntegrationTests.Features.System;

[Trait("Application", "Integration Tests")]
public class GetPingFeature_Test
{

    [Fact]
    public async Task Ping_ShouldPong()
    {
        var provider = IntegrationTestSetup.GetServices();


        var handler = new PingGetFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>()
        );
        var result = await handler.Handle(new PingGetFeature.Query(), CancellationToken.None);

        Assert.Equal("pong", result);
    }
}
