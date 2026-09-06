// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Accounts;
using tld15Server.Features.Shared.Identity;

namespace tld15ServerTests.Application.IntegrationTests.Features.Accounts;

[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class AccountsSearchFeature_Tests
{
    private static AccountsSearchFeature.Handler CreateHandler(IServiceProvider provider)
    {
        return new AccountsSearchFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>()
        );
    }

    private static AccountsPostFeature.Handler CreateAccountHandler(IServiceProvider provider)
    {
        return new AccountsPostFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<ApiKeyService>()
        );
    }

    // The login drives the order of the search, so it carries the index of the account.
    private static async Task CreateAccountsAsync(IServiceProvider provider, int count)
    {
        for (var index = 0; index < count; index++)
        {
            await CreateAccountHandler(provider).Handle(new AccountsPostFeature.Command
            {
                Account = new SharedAccount
                {
                    Login = $"search-test-{index:D4}",
                    Password = "sa",
                    StatusId = Globals.Reference.AccountStatus.Enabled,
                    TypeId = Globals.Reference.AccountType.User,
                }
            }, CancellationToken.None);
        }
    }

    private static AccountsSearchFeature.Query Page(int page) => new()
    {
        LanguageId = "en",
        Page = page
    };

    // The migration seeds the automation account, and ResetAccountsAsync keeps it on purpose. Every
    // expectation below counts from what the reset leaves behind.
    private static async Task<int> ResetAndCountAsync(IServiceProvider provider)
    {
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var seeded = await CreateHandler(provider).Handle(Page(0), CancellationToken.None);

        return seeded.TotalCount;
    }

    [Fact]
    public async Task Search_WithFewerRowsThanOnePage_ReturnsEveryRow()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);
        await CreateAccountsAsync(provider, 3);

        var result = await CreateHandler(provider).Handle(Page(0), CancellationToken.None);

        Assert.Equal(baseline + 3, result.TotalCount);
        Assert.Equal(baseline + 3, result.Items.Count);
    }

    [Fact]
    public async Task Search_WithMoreRowsThanOnePage_CutsThePageAndKeepsTheTotal()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);

        var overflow = Globals.Pagination.PageSize + 4;
        await CreateAccountsAsync(provider, overflow - baseline);

        var result = await CreateHandler(provider).Handle(Page(0), CancellationToken.None);

        Assert.Equal(overflow, result.TotalCount);
        Assert.Equal(Globals.Pagination.PageSize, result.Items.Count);
    }

    [Fact]
    public async Task Search_OnTheSecondPage_ReturnsTheRemainder()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);

        var overflow = Globals.Pagination.PageSize + 4;
        await CreateAccountsAsync(provider, overflow - baseline);

        var result = await CreateHandler(provider).Handle(Page(1), CancellationToken.None);

        Assert.Equal(overflow, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
    }

    // A row that appears on two pages is the failure that an unordered query produces.
    [Fact]
    public async Task Search_AcrossTwoPages_RepeatsNoRow()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);
        await CreateAccountsAsync(provider, Globals.Pagination.PageSize + 4 - baseline);

        var first = await CreateHandler(provider).Handle(Page(0), CancellationToken.None);
        var second = await CreateHandler(provider).Handle(Page(1), CancellationToken.None);

        Assert.NotEmpty(second.Items);

        foreach (var item in second.Items)
        {
            Assert.DoesNotContain(first.Items, x => x.Id == item.Id);
        }
    }

    [Fact]
    public async Task Search_BeyondTheLastPage_ReturnsNoRowAndKeepsTheTotal()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);
        await CreateAccountsAsync(provider, 3);

        var result = await CreateHandler(provider).Handle(Page(9), CancellationToken.None);

        Assert.Equal(baseline + 3, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_WithNegativePage_ReadsTheFirstPage()
    {
        var provider = IntegrationTestSetup.GetServices();
        var baseline = await ResetAndCountAsync(provider);
        await CreateAccountsAsync(provider, 3);

        var result = await CreateHandler(provider).Handle(Page(-5), CancellationToken.None);

        Assert.Equal(baseline + 3, result.Items.Count);
    }
}
