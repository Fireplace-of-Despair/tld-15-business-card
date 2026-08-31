// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentMigrator;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2000_01_01_0003, "Added: Source offer")]
public sealed class V2000_01_01_0003_Added_Source_Feature : Migration
{
    public override void Up()
    {
        Insert.Feature("system.source", new Dictionary<string, string> {
            { "en", "System: Source offer" }, { "ja", "システム: ソース提供" } });
    }

    public override void Down()
    {
        Delete.Feature("system.source");
    }
}
