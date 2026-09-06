// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
