// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StainlessInfrastructure.Migrations;

namespace tld15ServerTests;

internal class DatabaseAssemblyFixture : IAsyncLifetime
{
    public Task DisposeAsync() => Task.CompletedTask;
    ValueTask IAsyncLifetime.InitializeAsync()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.Tests.json").Build();
        MigrationRunner.Up(config.GetConnectionString("PostgreSQL")!);

        return new ValueTask(Task.CompletedTask);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return new ValueTask(Task.CompletedTask);
    }
}
