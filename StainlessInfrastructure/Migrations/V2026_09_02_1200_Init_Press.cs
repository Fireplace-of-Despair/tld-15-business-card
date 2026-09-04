// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2026_09_02_1200, "Init: Press")]
public sealed class V2026_09_02_1200_Init_Press : Migration
{
    public override void Up()
    {
        Create.Table("press")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("url").AsString(1024).NotNullable()
            .WithColumn("poster_url").AsString(1024).NotNullable().WithDefaultValue(string.Empty)
            // When the mention was published where it was published. created_at belongs to the
            // trigger and says when this row appeared, which is a different day entirely.
            .WithColumn("published_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // The page orders by date and reads the whole table, so the index carries the sort.
        Create.Index("idx_press__published_at")
            .OnTable("press").InSchema(Globals.Schema.Business)
            .OnColumn("published_at").Descending();
        this.AttachLocalVersionTrigger("press", Globals.Schema.Business);

        Create.Table("press_translation")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("press_id").AsGuid().NotNullable()
                .ForeignKey("fk_press_translation_to_press", Globals.Schema.Business, "press", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_press_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("subtitle").AsString().NotNullable()
            .WithColumn("poster_alt").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // One translation per locale, and the lookup every card makes: mention plus language.
        Create.Index("idx_press_translation__press_id__language_id")
            .OnTable("press_translation").InSchema(Globals.Schema.Business)
            .OnColumn("press_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("press_translation", Globals.Schema.Business);

        Insert.Feature("press.list", new Dictionary<string, string> {
            { "en", "Press: List" }, { "ja", "メディア掲載：一覧" } });
        Insert.Feature("press.search", new Dictionary<string, string> {
            { "en", "Press: Search" }, { "ja", "メディア掲載：検索" } });
        Insert.Feature("press.get", new Dictionary<string, string> {
            { "en", "Press: Get" }, { "ja", "メディア掲載：取得" } });
        Insert.Feature("press.post", new Dictionary<string, string> {
            { "en", "Press: Create/Update" }, { "ja", "メディア掲載：作成/更新" } });
        Insert.Feature("press.delete", new Dictionary<string, string> {
            { "en", "Press: Delete" }, { "ja", "メディア掲載：削除" } });
    }

    public override void Down()
    {
        Delete.Feature("press.list");
        Delete.Feature("press.search");
        Delete.Feature("press.get");
        Delete.Feature("press.post");
        Delete.Feature("press.delete");

        Delete.Table("press_translation").InSchema(Globals.Schema.Business);
        Delete.Table("press").InSchema(Globals.Schema.Business);
    }
}
