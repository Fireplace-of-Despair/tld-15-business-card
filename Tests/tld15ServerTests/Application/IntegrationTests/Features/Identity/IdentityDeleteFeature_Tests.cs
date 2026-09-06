// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Features.Identity;
using tld15Server.Services;

namespace tld15ServerTests.Application.IntegrationTests.Features.Identity;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class IdentityDeleteFeature_Tests
{
    private IdentityPostFeature.Command _adminCommand = new()
    {
        Login = "sa",
        Password = "sa",
        AcceptEncoding = Guid.NewGuid().ToString(),
        AcceptLanguage = Guid.NewGuid().ToString(),
        UserAgent = Guid.NewGuid().ToString(),
        UserIP = Guid.NewGuid().ToString(),
    };

    [Fact]
    public async Task Logout_ShouldWin()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var contextIdentityFactory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        var identityPostFeature = new IdentityPostFeature.Handler
        (
            provider.GetRequiredService<IConfiguration>(),
            contextIdentityFactory,
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<LoginThrottle>()
        );

        var loginResult = await identityPostFeature.Handle(_adminCommand, CancellationToken.None);

        var identityDeleteFeature = new IdentityDeleteFeature.Handler
        (
            provider.GetRequiredService<CacheManager>()
        );

        var logoutResult = await identityDeleteFeature.Handle(new IdentityDeleteFeature.Query
        {
            SessionId = loginResult.Id
        }, CancellationToken.None);

        Assert.True(logoutResult);
    }
}
