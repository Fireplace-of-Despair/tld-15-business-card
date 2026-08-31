// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
