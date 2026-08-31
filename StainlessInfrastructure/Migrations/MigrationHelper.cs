// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using FluentMigrator;
using FluentMigrator.Builders.Delete;
using FluentMigrator.Builders.Insert;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

internal static class MigrationHelper
{
    internal static void AttachLatinOnlyConstraint(this Migration migrator, string table, string schema, string column)
    {
        migrator.Execute.Sql(@$"
            ALTER TABLE {schema}.{table}
                ADD CONSTRAINT chk__{schema}_{table}_{column}_only_latin
                CHECK ({column} ~ '^[a-z0-9_.-]+$');");
    }

    internal static void AttachLocalVersionTrigger(this Migration migrator, string table, string schema)
    {
        migrator.Execute.Sql(@$"
        CREATE TRIGGER on_before_update_or_insert_bump_local_version_in_{table}
        BEFORE INSERT OR UPDATE ON {schema}.{table}
            FOR EACH ROW
        EXECUTE FUNCTION {Globals.Schema.System}.bump_local_version_function();
        ");
    }

    internal static void AttachGlobalVersionTrigger(this Migration migrator, string table, string schema)
    {
        migrator.Execute.Sql(@$"
        CREATE TRIGGER on_before_update_or_insert_bump_global_version_in_{table}
        BEFORE INSERT OR UPDATE ON {schema}.{table}
            FOR EACH ROW
        EXECUTE FUNCTION {Globals.Schema.System}.bump_global_version_function();
        ");
    }

    internal static void FillBasicReferenceTable(this Migration migrator, string table, string schema, string fileName)
    {
        var json = File.ReadAllText(Path.Combine(Globals.ImportsLocation, fileName));
        var items = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ListImportReferenceModel);

        var tableId = $"{table}_id";

        foreach (var item in items ?? [])
        {
            var mainRow = new ExpandoObject();
            mainRow.TryAdd("id", item.Id);

            migrator.Insert.IntoTable(table)
                .InSchema(schema)
                .Row(mainRow);

            foreach (var translation in item.Translations)
            {
                var row = new ExpandoObject();
                row.TryAdd("id", Guid.NewGuid());
                row.TryAdd(tableId, item.Id);
                row.TryAdd("language_id", translation.LanguageId);
                row.TryAdd("name", translation.Name);

                migrator.Insert.IntoTable($"{table}_translation")
                    .InSchema(schema)
                    .Row(row);
            }
        }
    }

    internal static void FillWeightedReferenceTable(this Migration migrator, string table, string schema, string fileName)
    {
        var json = File.ReadAllText(Path.Combine(Globals.ImportsLocation, fileName));
        var items = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ListImportReferenceModelWeight);

        var tableId = $"{table}_id";

        foreach (var item in items ?? [])
        {
            var mainRow = new ExpandoObject();
            mainRow.TryAdd("id", item.Id);
            mainRow.TryAdd("weight", item.Weight);

            migrator.Insert.IntoTable(table)
                .InSchema(schema)
                .Row(mainRow);

            foreach (var translation in item.Translations)
            {
                var row = new ExpandoObject();
                row.TryAdd("id", Guid.NewGuid());
                row.TryAdd(tableId, item.Id);
                row.TryAdd("language_id", translation.LanguageId);
                row.TryAdd("name", translation.Name);

                migrator.Insert.IntoTable($"{table}_translation")
                    .InSchema(schema)
                    .Row(row);
            }
        }
    }

    internal static void Feature(this IInsertExpressionRoot insert, string id, Dictionary<string, string> translations)
    {
        insert.IntoTable("feature")
              .InSchema(Globals.Schema.Identity)
              .Row(new { id });

        foreach (var item in translations)
        {
            insert.IntoTable("feature_translation")
                  .InSchema(Globals.Schema.Identity)
                  .Row(new { id = Guid.NewGuid(), feature_id = id, language_id = item.Key, name = item.Value });
        }
    }

    internal static void Feature(this IDeleteExpressionRoot delete, string id)
    {
        delete.FromTable("feature")
              .InSchema(Globals.Schema.Identity)
              .Row(new { id });
    }
}
