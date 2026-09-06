// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2000_01_01_0001, "Init: References")]
public sealed class V2000_01_01_0001_Init_References : Migration
{
    public override void Up()
    {

        Create.Table("language")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsString(Globals.ColumnLength.LanguageId).NotNullable().PrimaryKey()
            .WithColumn("name").AsString(1024).NotNullable().Unique()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("language", Globals.Schema.Reference);
        this.AttachLatinOnlyConstraint("language", Globals.Schema.Reference, "id");

        Insert.IntoTable("language")
            .InSchema(Globals.Schema.Reference)
            .Row(new { id = "en", name = "English" });
        Insert.IntoTable("language")
            .InSchema(Globals.Schema.Reference)
            .Row(new { id = "ja", name = "日本語" });

        Create.Table("account_status")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsString(1024).NotNullable().PrimaryKey()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("account_status", Globals.Schema.Reference);
        this.AttachLatinOnlyConstraint("account_status", Globals.Schema.Reference, "id");

        Create.Table("account_status_translation")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("account_status_id").AsString(1024).NotNullable().ForeignKey("fk_account_status_translation_to_account_status", Globals.Schema.Reference, "account_status", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable().ForeignKey("fk_account_status_translation_to_language", Globals.Schema.Reference, "language", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(1024).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        Create.Index("idx_account_status_translation__account_status_id__language_id")
            .OnTable("account_status_translation").InSchema(Globals.Schema.Reference)
            .OnColumn("account_status_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("account_status_translation", Globals.Schema.Reference);

        this.FillBasicReferenceTable("account_status", Globals.Schema.Reference, "account_status.json");


        Create.Table("account_type")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsString(1024).NotNullable().PrimaryKey()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("account_type", Globals.Schema.Reference);
        this.AttachLatinOnlyConstraint("account_type", Globals.Schema.Reference, "id");

        Create.Table("account_type_translation")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("account_type_id").AsString(1024).NotNullable().ForeignKey("fk_account_type_translation_to_account_type", Globals.Schema.Reference, "account_type", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable().ForeignKey("fk_account_type_translation_to_language", Globals.Schema.Reference, "language", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(1024).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        Create.Index("idx_account_type_translation__account_type_id__language_id")
            .OnTable("account_type_translation").InSchema(Globals.Schema.Reference)
            .OnColumn("account_type_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("account_type_translation", Globals.Schema.Reference);

        this.FillBasicReferenceTable("account_type", Globals.Schema.Reference, "account_type.json");
    }

    public override void Down()
    {
        Delete.Table("account_type_translation").InSchema(Globals.Schema.Reference);
        Delete.Table("account_type").InSchema(Globals.Schema.Reference);
        Delete.Table("account_status_translation").InSchema(Globals.Schema.Reference);
        Delete.Table("account_status").InSchema(Globals.Schema.Reference);
        Delete.Table("language").InSchema(Globals.Schema.Reference);
    }
}
