// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2026_08_31_1336, "Init: Business")]
public sealed class V2026_08_31_1336_Init_Business : Migration
{
    public override void Up()
    {
        Create.Table("division")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsString(Globals.ColumnLength.DivisionId).PrimaryKey()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("division", Globals.Schema.Reference);

        Create.Table("division_translation")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("division_id").AsString(Globals.ColumnLength.DivisionId).NotNullable()
                .ForeignKey("fk_division_translation_to_division", Globals.Schema.Reference, "division", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_division_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // One name per locale, and the lookup every page makes: division plus language.
        Create.Index("idx_division_translation__division_id__language_id")
            .OnTable("division_translation").InSchema(Globals.Schema.Reference)
            .OnColumn("division_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("division_translation", Globals.Schema.Reference);
        this.FillBasicReferenceTable("division", Globals.Schema.Reference, "divisions.json");

        Create.Table("project_type")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsString(Globals.ColumnLength.ProjectTypeId).NotNullable().PrimaryKey()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("project_type", Globals.Schema.Reference);
        this.AttachLatinOnlyConstraint("project_type", Globals.Schema.Reference, "id");

        Create.Table("project_type_translation")
            .InSchema(Globals.Schema.Reference)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("project_type_id").AsString(Globals.ColumnLength.ProjectTypeId).NotNullable().ForeignKey("fk_project_type_translation_to_project_type", Globals.Schema.Reference, "project_type", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable().ForeignKey("fk_project_type_translation_to_language", Globals.Schema.Reference, "language", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("name").AsString(1024).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        Create.Index("idx_project_type_translation__project_type_id__language_id")
            .OnTable("project_type_translation").InSchema(Globals.Schema.Reference)
            .OnColumn("project_type_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("project_type_translation", Globals.Schema.Reference);
        this.FillBasicReferenceTable("project_type", Globals.Schema.Reference, "project_types.json");


        Create.Table("content")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsString(Globals.ColumnLength.ContentId).PrimaryKey()
            .WithColumn("poster_url").AsString(1024).Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("content", Globals.Schema.Business);

        Create.Table("content_translation")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("content_id").AsString(Globals.ColumnLength.ContentId).NotNullable()
                .ForeignKey("fk_content_translation_to_content", Globals.Schema.Business, "content", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_content_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("markdown").AsString().Nullable()
            .WithColumn("json").AsString().Nullable()
            .WithColumn("poster_alt").AsString().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // One translation per locale, and the lookup the home page makes: content plus language.
        Create.Index("idx_content_translation__content_id__language_id")
            .OnTable("content_translation").InSchema(Globals.Schema.Business)
            .OnColumn("content_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("content_translation", Globals.Schema.Business);
        this.FillBasicReferenceTable("content", Globals.Schema.Business, "content_types.json");

        Create.Table("project")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsString(Globals.ColumnLength.ProjectId).PrimaryKey()
            .WithColumn("division_id").AsString(Globals.ColumnLength.DivisionId).NotNullable()
                .ForeignKey("fk_division_id_to_division", Globals.Schema.Reference, "division", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("project_type_id").AsString(Globals.ColumnLength.ProjectTypeId).NotNullable()
                .ForeignKey("fk_project_type_id_to_project_type", Globals.Schema.Reference, "project_type", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("poster_url").AsString(1024).NotNullable().WithDefaultValue(string.Empty)
            .WithColumn("links_json").AsString().Nullable()
            // The date the work was published, which is the editor's to set: created_at says when the
            // row appeared, and an article carried over from the old site did not appear when it was
            // written. The cards read this one.
            .WithColumn("published_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // The home page filters by type and orders by date, so the index carries both and the read never sorts.
        Create.Index("idx_project__project_type_id__published_at")
            .OnTable("project").InSchema(Globals.Schema.Business)
            .OnColumn("project_type_id").Ascending()
            .OnColumn("published_at").Descending();
        // PostgreSQL indexes no foreign key on its own, and the cascade from division takes this direction.
        Create.Index("idx_project__division_id")
            .OnTable("project").InSchema(Globals.Schema.Business)
            .OnColumn("division_id").Ascending();
        this.AttachLocalVersionTrigger("project", Globals.Schema.Business);

        Create.Table("project_translation")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("project_id").AsString(Globals.ColumnLength.ProjectId).NotNullable()
                .ForeignKey("fk_project_translation_to_project", Globals.Schema.Business, "project", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_project_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("poster_alt").AsString().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("subtitle").AsString().NotNullable()
            .WithColumn("markdown").AsString().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        // One translation per locale, and the lookup every card makes: project plus language.
        Create.Index("idx_project_translation__project_id__language_id")
            .OnTable("project_translation").InSchema(Globals.Schema.Business)
            .OnColumn("project_id").Ascending()
            .OnColumn("language_id").Ascending()
            .WithOptions().Unique();
        this.AttachLocalVersionTrigger("project_translation", Globals.Schema.Business);

        Insert.Feature("content.get", new Dictionary<string, string> {
            { "en", "Content: Get" }, { "ja", "コンテンツ：取得" } });
        Insert.Feature("content.post", new Dictionary<string, string> {
            { "en", "Content: Create/Update" }, { "ja", "コンテンツ：作成/更新" } });
        Insert.Feature("content.search", new Dictionary<string, string> {
            { "en", "Content: Search" }, { "ja", "コンテンツ：検索" } });

        Insert.Feature("archive.get", new Dictionary<string, string> {
            { "en", "Archive: Get" }, { "ja", "アーカイブ：取得" } });

        Insert.Feature("project.get", new Dictionary<string, string> {
            { "en", "Project: Get" }, { "ja", "プロジェクト：取得" } });
        Insert.Feature("project.post", new Dictionary<string, string> {
            { "en", "Project: Create/Update" }, { "ja", "プロジェクト：作成/更新" } });
        Insert.Feature("project.delete", new Dictionary<string, string> {
            { "en", "Project: Delete" }, { "ja", "プロジェクト：削除" } });
        Insert.Feature("project.search", new Dictionary<string, string> {
            { "en", "Project: Search" }, { "ja", "プロジェクト：検索" } });
    }

    public override void Down()
    {
        Delete.Feature("content.get");
        Delete.Feature("content.post");
        Delete.Feature("content.search");

        Delete.Feature("archive.get");

        Delete.Feature("project.get");
        Delete.Feature("project.post");
        Delete.Feature("project.delete");
        Delete.Feature("project.search");

        Delete.Table("project_translation").InSchema(Globals.Schema.Business);
        Delete.Table("project").InSchema(Globals.Schema.Business);
        Delete.Table("content_translation").InSchema(Globals.Schema.Business);
        Delete.Table("content").InSchema(Globals.Schema.Business);

        Delete.Table("project_type_translation").InSchema(Globals.Schema.Reference);
        Delete.Table("project_type").InSchema(Globals.Schema.Reference);
        Delete.Table("division_translation").InSchema(Globals.Schema.Reference);
        Delete.Table("division").InSchema(Globals.Schema.Reference);
    }
}
