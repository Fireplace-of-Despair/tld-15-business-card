// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
