using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Business;

[Table("content", Schema = Globals.Schema.Business)]
public sealed class Content : IVersionLocal
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<ContentTranslation> Translations { get; set; } = [];
}

[Table("content_translation", Schema = Globals.Schema.Business)]
public sealed class ContentTranslation : IVersionLocal
{
    [Key, Column("id")]
    public required Guid Id { get; set; }

    [Column("content_id")]
    public required string ContentId { get; set; }

    [Column("language_id")]
    public required string LanguageId { get; set; }

    [Column("data")]
    public string Data { get; set; } = string.Empty;

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
