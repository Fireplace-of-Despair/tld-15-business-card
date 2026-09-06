// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
