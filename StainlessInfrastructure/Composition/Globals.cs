// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentMigrator.Runner.VersionTableInfo;

namespace StainlessInfrastructure.Composition;

[ExcludeFromCodeCoverage]
internal static class Globals
{
    internal static string ImportsLocation => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".import");

    internal static class ColumnLength
    {
        internal const int FeatureId = 64;
        internal const int ContentId = 64;
        internal const int DivisionId = 3;
        internal const int ProjectId = 64;
        internal const int ProjectTypeId = 64;
        internal const int LanguageId = 4;
    }

    internal static class Schema
    {
        internal const string System = "system";
        internal const string Identity = "identity";
        internal const string Reference = "reference";
        internal const string Business = "business";
        internal const string Settings = "settings";
        internal const string Archive = "archive";
    }

    [VersionTableMetaData]
    public sealed class VersionTable : IVersionTableMetaData
    {
        public bool CreateWithPrimaryKey => false;
        public bool OwnsSchema => true;
        public object? ApplicationContext { get; set; }
        public string AppliedOnColumnName => "created_at";
        public string ColumnName => "version";
        public string DescriptionColumnName => "description";
        public string SchemaName => Schema.System;
        public string TableName => "version";
        public string UniqueIndexName => "version__idx";
    }
}
