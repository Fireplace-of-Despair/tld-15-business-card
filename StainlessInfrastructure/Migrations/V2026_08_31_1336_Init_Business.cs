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
                .ForeignKey("fk_feature_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("division_translation", Globals.Schema.Reference);
        this.FillBasicReferenceTable("division", Globals.Schema.Reference, "divisions.json");


        Create.Table("content")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsString(Globals.ColumnLength.ContentId).PrimaryKey()
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
                .ForeignKey("fk_feature_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("html").AsString().Nullable()
            .WithColumn("json").AsString().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("content_translation", Globals.Schema.Business);
        this.FillBasicReferenceTable("content", Globals.Schema.Business, "content_types.json");

        Create.Table("project")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsString(Globals.ColumnLength.ProjectId).PrimaryKey()
            .WithColumn("division_id").AsString(3)
            .WithColumn("poster_url").AsString(100)
            .WithColumn("links_json").AsString()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("project", Globals.Schema.Business);

        Create.Table("project_translation")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("project_id").AsString(Globals.ColumnLength.ProjectId).NotNullable()
                .ForeignKey("fk_project_translation_to_project", Globals.Schema.Business, "project", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_feature_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("poster_alt").AsString().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("subtitle").AsString().NotNullable()
            .WithColumn("content_html").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("project_translation", Globals.Schema.Business);

        Create.Table("article")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsString(Globals.ColumnLength.ProjectId).PrimaryKey()
            .WithColumn("division_id").AsString(3)
            .WithColumn("poster_url").AsString(100)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("article", Globals.Schema.Business);

        Create.Table("article_translation")
            .InSchema(Globals.Schema.Business)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("article_id").AsString(Globals.ColumnLength.ProjectId).NotNullable()
                .ForeignKey("fk_article_translation_to_article", Globals.Schema.Business, "article", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("language_id").AsString(Globals.ColumnLength.LanguageId).NotNullable()
                .ForeignKey("fk_feature_translation_to_language", Globals.Schema.Reference, "language", "id")
                .OnDelete(Rule.Cascade)
            .WithColumn("poster_alt").AsString().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("subtitle").AsString().NotNullable()
            .WithColumn("content_html").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("version_local").AsInt64().NotNullable().WithDefaultValue(0);
        this.AttachLocalVersionTrigger("article_translation", Globals.Schema.Business);

        Insert.Feature("content.get", new Dictionary<string, string> {
            { "en", "Content: Get" }, { "ja", "コンテンツ：取得" } });
        Insert.Feature("content.post", new Dictionary<string, string> {
            { "en", "Content: Create/Update" }, { "ja", "コンテンツ：作成/更新" } });
        Insert.Feature("content.search", new Dictionary<string, string> {
            { "en", "Content: Search" }, { "ja", "コンテンツ：検索" } });

        Insert.Feature("project.get", new Dictionary<string, string> {
            { "en", "Project: Get" }, { "ja", "プロジェクト：取得" } });
        Insert.Feature("project.post", new Dictionary<string, string> {
            { "en", "Project: Create/Update" }, { "ja", "プロジェクト：作成/更新" } });
        Insert.Feature("project.search", new Dictionary<string, string> {
            { "en", "Project: Seach" }, { "ja", "プロジェクト：検索" } });

        Insert.Feature("article.get", new Dictionary<string, string> {
            { "en", "Article: Get" }, { "ja", "記事：取得" } });
        Insert.Feature("article.post", new Dictionary<string, string> {
            { "en", "Article: Create/Update" }, { "ja", "記事：作成/更新" } });
        Insert.Feature("article.seach", new Dictionary<string, string> {
            { "en", "Article: Create/Update" }, { "ja", "記事：検索" } });
    }

    public override void Down()
    {
        //TODO: down
    }
}
