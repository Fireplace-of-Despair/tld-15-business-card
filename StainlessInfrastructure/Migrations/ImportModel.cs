// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
