// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2000_01_01_0002, "Init: Identity")]
public sealed class V2000_01_01_0002_Init_Identity : Migration
{
    public override void Up()
    {
        Create.Table("account")
            .InSchema(Globals.Schema.Identity)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("login").AsString(1024).NotNullable().Unique()
            .WithColumn("password").AsString(1024).NotNullable()
            .WithColumn("salt").AsString(1024).NotNullable()
            .WithColumn("account_status_id").AsString(1024).NotNullable()
                .ForeignKey("fk_account_to_account_status", Globals.Schema.Reference, "account_status", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("account_type_id").AsString(1024).NotNullable()
                .ForeignKey("fk_account_to_account_type", Globals.Schema.Reference, "account_type", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("account", Globals.Schema.Identity);

        Create.Table("api_key")
            .InSchema(Globals.Schema.Identity)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().NotNullable()
                .ForeignKey("fk_api_key_to_account", Globals.Schema.Identity, "account", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("key_hash").AsString(1024).NotNullable().Unique()
            .WithColumn("last_used_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        Create.Index("idx_api_key__account_id")
            .OnTable("api_key").InSchema(Globals.Schema.Identity)
            .OnColumn("account_id").Ascending();
        this.AttachLocalVersionTrigger("api_key", Globals.Schema.Identity);


        Create.Table("feature")
            .InSchema(Globals.Schema.Identity)
            .WithColumn("id").AsString(Globals.ColumnLength.FeatureId).NotNullable().PrimaryKey()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("feature", Globals.Schema.Identity);

        Create.Table("feature_translation")
            .InSchema(Globals.Schema.Identity)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("feature_id").AsString(Globals.ColumnLength.FeatureId).NotNullable()
                .ForeignKey("fk_feature_translation_to_feature", Globals.Schema.Identity, "feature", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_feature_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("name").AsString(1024).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        Create.Index("idx_feature_translation__feature_id__language_id")
            .OnTable("feature_translation").InSchema(Globals.Schema.Identity)
            .OnColumn("feature_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("feature_translation", Globals.Schema.Identity);


        Create.Table("account_to_feature")
            .InSchema(Globals.Schema.Identity)
            .WithColumn("account_id").AsGuid().NotNullable()
                .ForeignKey("fk_account_to_feature_to_account", Globals.Schema.Identity, "account", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("feature_id").AsString(Globals.ColumnLength.FeatureId).NotNullable()
                .ForeignKey("fk_account_to_feature_to_feature", Globals.Schema.Identity, "feature", "id")
                .OnDelete(Rule.Cascade);
        // The pair is the key of the join table. It also serves the read direction that the start-up
        // and every permission check take: the features of one account.
        Create.Index("idx_account_to_feature__account_id__feature_id")
            .OnTable("account_to_feature").InSchema(Globals.Schema.Identity)
            .OnColumn("account_id").Ascending()
            .OnColumn("feature_id").Ascending()
            .WithOptions().Unique();
        // The other direction, which the cascade from feature takes.
        Create.Index("idx_account_to_feature__feature_id")
            .OnTable("account_to_feature").InSchema(Globals.Schema.Identity)
            .OnColumn("feature_id").Ascending();



        Insert.Feature("system.ping", new Dictionary<string, string> {
            { "en", "System: Ping" }, { "ja", "システム: ピン" } });

        Insert.Feature("identity.post", new Dictionary<string, string> {
            { "en", "Identity: Login" }, { "ja", "アカウント：サインイン" } });
        Insert.Feature("identity.delete", new Dictionary<string, string> {
            { "en", "Identity: Logout" }, { "ja", "アカウント：サインアウト" } });

        Insert.Feature("accounts.search", new Dictionary<string, string> {
            { "en", "Accounts: Search" }, { "ja", "アカウント：検索" } });
        Insert.Feature("accounts.get", new Dictionary<string, string> {
            { "en", "Accounts: Read" }, { "ja", "アカウント：表示" } });
        Insert.Feature("accounts.post", new Dictionary<string, string> {
            { "en", "Accounts: Create/Update" }, { "ja", "アカウント：作成/更新" } });
        Insert.Feature("accounts.delete", new Dictionary<string, string> {
            { "en", "Accounts: Delete" }, { "ja", "アカウント：削除" } });

        Insert.Feature("sessions.search", new Dictionary<string, string> {
            { "en", "Sessions: Search" }, { "ja", "セッション：検索" } });
        Insert.Feature("sessions.delete", new Dictionary<string, string> {
            { "en", "Sessions: Delete" }, { "ja", "セッション：削除" } });


        Insert.Feature("profile.get", new Dictionary<string, string> {
            { "en", "Profile: View" }, { "ja", "横顔：表示" } });
        Insert.Feature("profile.put", new Dictionary<string, string> {
            { "en", "Profile: Update" }, { "ja", "横顔：更新" } });
        Insert.Feature("profile.delete", new Dictionary<string, string> {
            { "en", "Profile: Delete" }, { "ja", "横顔：削除" } });

        Insert.IntoTable("account")
            .InSchema(Globals.Schema.Identity)
            .Row(new
            {
                id = Guid.NewGuid(),
                login = "automation",
                password = "0x0",
                salt = "0x0",
                account_status_id = "disabled",
                account_type_id = "system_automation"
            });
    }

    public override void Down()
    {
        Delete.Feature("system.ping");

        Delete.Feature("identity.post");
        Delete.Feature("identity.delete");

        Delete.Feature("accounts.search");
        Delete.Feature("accounts.get");
        Delete.Feature("accounts.post");
        Delete.Feature("accounts.delete");

        Delete.Feature("sessions.search");
        Delete.Feature("sessions.delete");

        Delete.Feature("profile.get");
        Delete.Feature("profile.put");
        Delete.Feature("profile.delete");

        Delete.Table("account_to_feature").InSchema(Globals.Schema.Identity);
        Delete.Table("feature_translation").InSchema(Globals.Schema.Identity);
        Delete.Table("api_key").InSchema(Globals.Schema.Identity);
        Delete.Table("feature").InSchema(Globals.Schema.Identity);
        Delete.Table("account").InSchema(Globals.Schema.Identity);
    }
}
