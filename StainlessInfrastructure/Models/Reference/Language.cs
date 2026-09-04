// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Reference;

[Table("language", Schema = Globals.Schema.Reference)]
public sealed class Language : IVersionLocal
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
