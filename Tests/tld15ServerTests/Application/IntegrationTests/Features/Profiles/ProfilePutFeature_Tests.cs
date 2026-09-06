// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore.Exceptions;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Features.Accounts;
using tld15Server.Features.Profiles;
using tld15Server.Features.Shared.Identity;

namespace tld15ServerTests.Application.IntegrationTests.Features.Profiles;

// Claude - Opus 5
[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class ProfilePutFeature_Tests
{
    private const string OriginalPassword = "original-password";

    private static ProfilePutFeature.Handler CreateHandler(IServiceProvider provider)
    {
        return new ProfilePutFeature.Handler
        (
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),

            provider.GetRequiredService<ApiKeyService>()
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

    private static async Task<SharedAccount> CreateAccountAsync(IServiceProvider provider)
    {
        var account = new SharedAccount
        {
            Login = "profile-test-" + Guid.NewGuid().ToString("N"),
            Password = OriginalPassword,
            StatusId = tld15Server.Composition.Globals.Reference.AccountStatus.Enabled,
            TypeId = tld15Server.Composition.Globals.Reference.AccountType.User,
        };

        var created = await CreateAccountHandler(provider)
            .Handle(new AccountsPostFeature.Command { Account = account }, CancellationToken.None);

        account.Id = created.Id;
        account.VersionLocal = await StoredVersionAsync(provider, created.Id);
        account.Password = null;

        return account;
    }

    // version_local is owned by a database trigger, so the value to send back has to be read.
    private static async Task<long> StoredVersionAsync(IServiceProvider provider, Guid accountId)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        using var context = await factory.CreateDbContextAsync(CancellationToken.None);

        return await context.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => x.VersionLocal)
            .FirstAsync(CancellationToken.None);
    }

    private static async Task<bool> PasswordMatchesAsync(IServiceProvider provider, Guid accountId, string password)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        using var context = await factory.CreateDbContextAsync(CancellationToken.None);

        var stored = await context.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => new { x.Password, x.Salt })
            .FirstAsync(CancellationToken.None);

        return provider.GetRequiredService<HashingService>().Hash(password, stored.Salt).HexHash == stored.Password;
    }

    [Fact]
    public async Task Put_WithTheCurrentPassword_ChangesIt()
    {
        var provider = IntegrationTestSetup.GetServices();
        var account = await CreateAccountAsync(provider);
        account.Password = "replacement-password";

        await CreateHandler(provider).Handle(
            new ProfilePutFeature.Command
            {
                AccountId = account.Id!.Value,
                Account = account,
                CurrentPassword = OriginalPassword,
            },
            CancellationToken.None);

        Assert.True(await PasswordMatchesAsync(provider, account.Id!.Value, "replacement-password"));
    }

    [Fact]
    public async Task Put_WithTheWrongCurrentPassword_ThrowsAndKeepsThePassword()
    {
        var provider = IntegrationTestSetup.GetServices();
        var account = await CreateAccountAsync(provider);
        account.Password = "replacement-password";

        var incident = await Assert.ThrowsAsync<IncidentException>(() =>
            CreateHandler(provider).Handle(
                new ProfilePutFeature.Command
                {
                    AccountId = account.Id!.Value,
                    Account = account,
                    CurrentPassword = "not-the-password",
                },
                CancellationToken.None).AsTask());

        Assert.Equal(IncidentCode.WrongPassword, incident.Code);
        Assert.True(await PasswordMatchesAsync(provider, account.Id!.Value, OriginalPassword));
    }

    /// <summary>
    /// A held session is what this blocks. Without the password in use, the session alone must not
    /// replace it.
    /// </summary>
    [Fact]
    public async Task Put_WithoutTheCurrentPassword_ThrowsAndKeepsThePassword()
    {
        var provider = IntegrationTestSetup.GetServices();
        var account = await CreateAccountAsync(provider);
        account.Password = "replacement-password";

        var incident = await Assert.ThrowsAsync<IncidentException>(() =>
            CreateHandler(provider).Handle(
                new ProfilePutFeature.Command
                {
                    AccountId = account.Id!.Value,
                    Account = account,
                    CurrentPassword = null,
                },
                CancellationToken.None).AsTask());

        Assert.Equal(IncidentCode.WrongPassword, incident.Code);
        Assert.True(await PasswordMatchesAsync(provider, account.Id!.Value, OriginalPassword));
    }

    // Renaming, issuing a key or dropping one leaves the password alone, so nothing is confirmed.
    [Fact]
    public async Task Put_WithoutANewPassword_NeedsNoConfirmation()
    {
        var provider = IntegrationTestSetup.GetServices();
        var account = await CreateAccountAsync(provider);
        account.Login += "-renamed";
        account.ApiKeys.Add(new SharedApiKey());

        var result = await CreateHandler(provider).Handle(
            new ProfilePutFeature.Command { AccountId = account.Id!.Value, Account = account },
            CancellationToken.None);

        Assert.True(await PasswordMatchesAsync(provider, account.Id!.Value, OriginalPassword));
    }

    // The administrator reset on the account page sets a password for somebody else and cannot know
    // the one in use, so that path stays as it was.
    [Fact]
    public async Task AccountsPost_StillChangesThePasswordWithoutConfirmation()
    {
        var provider = IntegrationTestSetup.GetServices();
        var account = await CreateAccountAsync(provider);
        account.Password = "reset-by-administrator";

        await CreateAccountHandler(provider).Handle(
            new AccountsPostFeature.Command { Account = account },
            CancellationToken.None);

        Assert.True(await PasswordMatchesAsync(provider, account.Id!.Value, "reset-by-administrator"));
    }
}
