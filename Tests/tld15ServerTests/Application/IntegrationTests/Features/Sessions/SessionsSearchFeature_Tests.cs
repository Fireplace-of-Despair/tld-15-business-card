// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using tld15Server.Composition;
using tld15Server.Features.Sessions;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15ServerTests.Application.IntegrationTests.Features.Sessions;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class SessionsSearchFeature_Tests
{
    private static readonly DateTimeOffset _expiration = DateTimeOffset.UtcNow.AddMinutes(480);

    private static SharedSession CreateSession(int index) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        Login = $"search-{index:D4}",
        Features = [],
        UserAgent = "xunit",
        UserIP = "127.0.0.1",
        AcceptLanguage = "en",
        AcceptEncoding = "gzip",
        // The search orders by the newest first, so each session needs its own moment.
        CreatedAt = DateTimeOffset.UtcNow.AddSeconds(index),
        ExpiresAt = _expiration
    };

    private static CacheManager FillCache(IServiceProvider provider, int count)
    {
        var cache = provider.GetRequiredService<CacheManager>();

        for (var index = 0; index < count; index++)
        {
            cache.Save(CreateSession(index), _expiration);
        }

        return cache;
    }

    [Fact]
    public async Task Search_WithMoreSessionsThanOnePage_CutsThePageAndKeepsTheTotal()
    {
        var provider = IntegrationTestSetup.GetServices();
        var overflow = Globals.Pagination.PageSize + 4;
        FillCache(provider, overflow);

        var result = await new SessionsSearchFeature.Handler(provider.GetRequiredService<CacheManager>())
            .Handle(new SessionsSearchFeature.Query { Page = 0 }, CancellationToken.None);

        Assert.Equal(overflow, result.TotalCount);
        Assert.Equal(Globals.Pagination.PageSize, result.Sessions.Count);
    }

    [Fact]
    public async Task Search_OnTheSecondPage_ReturnsTheRemainder()
    {
        var provider = IntegrationTestSetup.GetServices();
        var overflow = Globals.Pagination.PageSize + 4;
        FillCache(provider, overflow);

        var handler = new SessionsSearchFeature.Handler(provider.GetRequiredService<CacheManager>());

        var first = await handler.Handle(new SessionsSearchFeature.Query { Page = 0 }, CancellationToken.None);
        var second = await handler.Handle(new SessionsSearchFeature.Query { Page = 1 }, CancellationToken.None);

        Assert.Equal(4, second.Sessions.Count);

        foreach (var session in second.Sessions)
        {
            Assert.DoesNotContain(first.Sessions, x => x.Id == session.Id);
        }
    }

    [Fact]
    public async Task Search_BeyondTheLastPage_ReturnsNoSession()
    {
        var provider = IntegrationTestSetup.GetServices();
        FillCache(provider, 3);

        var result = await new SessionsSearchFeature.Handler(provider.GetRequiredService<CacheManager>())
            .Handle(new SessionsSearchFeature.Query { Page = 99 }, CancellationToken.None);

        Assert.Empty(result.Sessions);
    }
}
