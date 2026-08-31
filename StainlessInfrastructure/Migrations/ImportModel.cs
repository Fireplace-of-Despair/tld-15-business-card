// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StainlessInfrastructure.Migrations;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<ImportReferenceModel>))]
[JsonSerializable(typeof(List<ImportReferenceModelWeight>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

internal class ImportReferenceModelWeight
{
    public required string Id { get; set; }
    public required int Weight { get; set; }
    public required List<ImportTranslationModel> Translations { get; set; }
}

internal class ImportReferenceModel
{
    public required string Id { get; set; }
    public required List<ImportTranslationModel> Translations { get; set; }
}

internal class ImportTranslationModel
{
    public required string LanguageId { get; set; }
    public required string Name { get; set; }
}
