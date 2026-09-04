// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StainlessInfrastructure;

namespace tld15ServerTests;

/// <summary>
/// Integration tests share one mutable database, so the classes that mutate accounts must not overlap.
/// Tests outside this collection (pure unit tests, ping) still run in parallel.
/// </summary>
internal static class DatabaseCollection
{
    internal const string Name = "Database";
}

internal class IntegrationTestSetup
{
    /// <summary>
    /// Drops every non-automation account, restoring the "empty database" state that
    /// <c>IdentityPostFeature.CreateFirstUserAsync</c> needs to bootstrap the admin account.
    /// Without it, any account another test leaves behind silently suppresses the bootstrap.
    /// </summary>
    public static async Task ResetAccountsAsync(IServiceProvider provider)
    {
        var contextIdentityFactory = provider.GetRequiredService<IDbContextFactory<DataContextIdentity>>();

        using var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(CancellationToken.None);
        await contextIdentity.Accounts
            .Where(x => x.AccountTypeId != tld15Server.Composition.Globals.Reference.AccountType.Automation)
            .ExecuteDeleteAsync(CancellationToken.None);
    }

    public static IServiceProvider GetServices()
    {
        var builder = WebApplication.CreateBuilder();

        var config = new ConfigurationBuilder().AddJsonFile("appsettings.Tests.json").Build();
        builder.Services.AddSingleton<IConfiguration>(config);

        //
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));


        //StainlessInfrastructure
        var connectionString = config.GetConnectionString("PostgreSQL")!;
        StainlessInfrastructure.Composition.ServiceInjection.AddDatabase(builder, connectionString);

        //StainlessCore
        StainlessCore.Composition.ServiceInjection.InjectCore(builder);

        //Application:
        tld15Server.Composition.ServiceInjection.AddMediator(builder);
        tld15Server.Composition.ServiceInjection.AddAuthentication(builder, config);
        tld15Server.Composition.ServiceInjection.AddHostedService(builder);
        tld15Server.Composition.ServiceInjection.AddGeneral(builder);

        var app = builder.Build();

        return app.Services;
    }

}
