// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
