// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StainlessCore;
using StainlessCore.Exceptions;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Features.Identity;
using tld15Server.Services;

namespace tld15ServerTests.Application.IntegrationTests.Features.Identity;


[Trait("Application", "Integration Tests")]
[Collection(DatabaseCollection.Name)]
public class IdentityPostFeature_Tests
{
    private readonly IdentityPostFeature.Command _adminCommand = new()
    {
        Login = "sa",
        Password = "sa",
        AcceptEncoding = Guid.NewGuid().ToString(),
        AcceptLanguage = Guid.NewGuid().ToString(),
        UserAgent = Guid.NewGuid().ToString(),
        UserIP = Guid.NewGuid().ToString(),
    };

    [Fact]
    public async Task EmptyDatabase_First_Account_ShouldWin()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var contextIdentityFactory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        IdentityPostFeature.Handler handler = new IdentityPostFeature.Handler
        (
            provider.GetRequiredService<IConfiguration>(),
            contextIdentityFactory,
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<LoginThrottle>()
        );

        var result = await handler.Handle(_adminCommand, CancellationToken.None);

        var tokenLifetime =
            provider.GetRequiredService<IConfiguration>()
            .GetSection(tld15Server.Composition.Globals.Security.CookieExpiration)
            .Get<int>();

        var permissions = new List<string>();
        using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(CancellationToken.None))
        {
            permissions = await contextIdentity.Features
            .Select(x => x.Id)
            .ToListAsync(CancellationToken.None);
        }

        Assert.NotNull(result);
        Assert.Equivalent(permissions, result.Features);
        Assert.Equal(_adminCommand.Login, result.Login);
        Assert.Equal(_adminCommand.AcceptEncoding, result.AcceptEncoding);
        Assert.Equal(_adminCommand.AcceptLanguage, result.AcceptLanguage);
        Assert.Equal(_adminCommand.UserAgent, result.UserAgent);
        Assert.Equal(_adminCommand.UserIP, result.UserIP);
    }

    [Fact]
    public async Task Wrong_Password_ShouldFail()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var contextIdentityFactory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        IdentityPostFeature.Handler handler = new IdentityPostFeature.Handler
        (
            provider.GetRequiredService<IConfiguration>(),
            contextIdentityFactory,
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<LoginThrottle>()
        );

        //Execute 1 (ensure that admin is there)
        await handler.Handle(_adminCommand, CancellationToken.None);

        await Assert.ThrowsAsync<IncidentException>
        (
            async () => await handler.Handle(new IdentityPostFeature.Command
            {
                Login = _adminCommand.Login,
                Password = "wrong password",
                AcceptEncoding = _adminCommand.AcceptEncoding,
                AcceptLanguage = _adminCommand.AcceptLanguage,
                UserAgent = _adminCommand.UserAgent,
                UserIP = _adminCommand.UserIP,
            }, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Wrong_Login_ShouldFail()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var contextIdentityFactory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();
        IdentityPostFeature.Handler handler = new IdentityPostFeature.Handler
        (
            provider.GetRequiredService<IConfiguration>(),
            contextIdentityFactory,
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<LoginThrottle>()
        );

        //Execute 1 (ensure that admin is there)
        await handler.Handle(_adminCommand, CancellationToken.None);

        await Assert.ThrowsAsync<IncidentException>
        (
            async () => await handler.Handle(new IdentityPostFeature.Command
            {
                Login = Guid.NewGuid().ToString(),
                Password = _adminCommand.Password,
                AcceptEncoding = _adminCommand.AcceptEncoding,
                AcceptLanguage = _adminCommand.AcceptLanguage,
                UserAgent = _adminCommand.UserAgent,
                UserIP = _adminCommand.UserIP,
            }, CancellationToken.None)
        );
    }

    // An unknown login and a known login with a wrong password must be indistinguishable to the caller,
    // otherwise the login form doubles as a "does this account exist?" oracle.
    [Fact]
    public async Task Wrong_Login_And_Wrong_Password_ReportTheSameIncident()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var handler = CreateHandler(provider);
        await handler.Handle(_adminCommand, CancellationToken.None);

        var unknownLogin = await Assert.ThrowsAsync<IncidentException>(() =>
            handler.Handle(CommandWith(login: Guid.NewGuid().ToString(), password: _adminCommand.Password),
                CancellationToken.None).AsTask());

        var wrongPassword = await Assert.ThrowsAsync<IncidentException>(() =>
            handler.Handle(CommandWith(login: _adminCommand.Login, password: Guid.NewGuid().ToString()),
                CancellationToken.None).AsTask());

        Assert.Equal(IncidentCode.WrongPassword, unknownLogin.Code);
        Assert.Equal(IncidentCode.WrongPassword, wrongPassword.Code);
    }

    [Fact]
    public async Task RepeatedFailures_LockTheLoginOut()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var handler = CreateHandler(provider);
        await handler.Handle(_adminCommand, CancellationToken.None);

        var maxAttempts = provider.GetRequiredService<LoginThrottle>().MaxAttemptsPerLogin;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var failure = await Assert.ThrowsAsync<IncidentException>(() =>
                handler.Handle(CommandWith(_adminCommand.Login, Guid.NewGuid().ToString()),
                    CancellationToken.None).AsTask());

            Assert.Equal(IncidentCode.WrongPassword, failure.Code);
        }

        // Over the threshold the correct password must not get in either, or the lockout is decorative.
        var lockedOut = await Assert.ThrowsAsync<IncidentException>(() =>
            handler.Handle(_adminCommand, CancellationToken.None).AsTask());

        Assert.Equal(IncidentCode.TooManyAttempts, lockedOut.Code);
    }

    [Fact]
    public async Task SuccessfulLogin_ClearsEarlierFailures()
    {
        var provider = IntegrationTestSetup.GetServices();
        await IntegrationTestSetup.ResetAccountsAsync(provider);

        var handler = CreateHandler(provider);
        await handler.Handle(_adminCommand, CancellationToken.None);

        var maxAttempts = provider.GetRequiredService<LoginThrottle>().MaxAttemptsPerLogin;

        // One short of the threshold, then a success: the counter must reset rather than carry over.
        for (var attempt = 0; attempt < maxAttempts - 1; attempt++)
        {
            await Assert.ThrowsAsync<IncidentException>(() =>
                handler.Handle(CommandWith(_adminCommand.Login, Guid.NewGuid().ToString()),
                    CancellationToken.None).AsTask());
        }

        await handler.Handle(_adminCommand, CancellationToken.None);

        var failure = await Assert.ThrowsAsync<IncidentException>(() =>
            handler.Handle(CommandWith(_adminCommand.Login, Guid.NewGuid().ToString()),
                CancellationToken.None).AsTask());

        Assert.Equal(IncidentCode.WrongPassword, failure.Code);
    }

    private static IdentityPostFeature.Handler CreateHandler(IServiceProvider provider)
    {
        return new IdentityPostFeature.Handler
        (
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>(),
            provider.GetRequiredService<HashingService>(),
            provider.GetRequiredService<CacheManager>(),
            provider.GetRequiredService<LoginThrottle>()
        );
    }

    private IdentityPostFeature.Command CommandWith(string login, string password) => new()
    {
        Login = login,
        Password = password,
        AcceptEncoding = _adminCommand.AcceptEncoding,
        AcceptLanguage = _adminCommand.AcceptLanguage,
        UserAgent = _adminCommand.UserAgent,
        UserIP = _adminCommand.UserIP,
    };
}
