// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Migrations;

[ExcludeFromCodeCoverage]
[Migration(2000_01_01_0000, "Init: Schema")]
public sealed class V2000_01_01_0000_Init_Schema : Migration
{
    public override void Up()
    {
        if (Schema.Schema("public").Exists())
        {
            Delete.Schema("public");
        }

        Create.Schema(Globals.Schema.Archive);
        Create.Schema(Globals.Schema.Reference);
        Create.Schema(Globals.Schema.Identity);
        Create.Schema(Globals.Schema.Business);
        Create.Schema(Globals.Schema.Settings);

        Execute.Sql(@$"CREATE SEQUENCE {Globals.Schema.System}.global_version_seq;");

        Execute.Sql(@$"
        CREATE OR REPLACE FUNCTION {Globals.Schema.System}.bump_local_version_function()
        RETURNS TRIGGER AS $$
        BEGIN
            IF TG_OP = 'INSERT' THEN
                NEW.created_at = timezone('UTC', now());
                NEW.version_local = 0;
            ELSE
                NEW.version_local = OLD.version_local + 1;
            END IF;

            NEW.updated_at = timezone('UTC', now());
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        ");

        Execute.Sql(@$"
        CREATE OR REPLACE FUNCTION {Globals.Schema.System}.bump_global_version_function()
        RETURNS TRIGGER AS $$
        BEGIN
            IF TG_OP = 'INSERT' THEN
                NEW.created_at = timezone('UTC', now());
            END IF;

            NEW.updated_at = timezone('UTC', now());
            NEW.version_global = nextval('{Globals.Schema.System}.global_version_seq');
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        ");
    }

    public override void Down()
    {
        Delete.Schema(Globals.Schema.Archive);
        Delete.Schema(Globals.Schema.Reference);
        Delete.Schema(Globals.Schema.Identity);
        Delete.Schema(Globals.Schema.Business);
        Delete.Schema(Globals.Schema.Settings);
        Create.Schema(Globals.Schema.System);
        Execute.Sql(@$"DROP SEQUENCE {Globals.Schema.System}.global_version_seq;");

        Create.Schema("public");
    }
}
