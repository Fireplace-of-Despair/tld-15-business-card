// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

public static class MigrationRunner
{
    public static void Up(string connectionString)
    {
        using (var scope = CreateServices(connectionString).CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.ListMigrations();
            runner.MigrateUp();
        }
    }

    public static void Down(string connectionString, long version)
    {
        using (var scope = CreateServices(connectionString).CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.ListMigrations();
            runner.MigrateDown(version);
        }
    }

    private static ServiceProvider CreateServices(string connectionString)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .WithVersionTable(new Globals.VersionTable())
                .ScanIn(typeof(MigrationRunner).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
    }
}
