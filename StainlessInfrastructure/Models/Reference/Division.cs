using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Reference;

[Table("division", Schema = Globals.Schema.Reference)]
public sealed class Division : IVersionLocal
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<DivisionTranslation> Translations { get; set; } = [];
}

[Table("division_translation", Schema = Globals.Schema.Reference)]
public sealed class DivisionTranslation : IVersionLocal
{
    [Key, Column("id")]
    public required Guid Id { get; set; }

    [Column("division_id")]
    public required string DivisionId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("language_id")]
    public required string LanguageId { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
