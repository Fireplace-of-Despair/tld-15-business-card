using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Business;

[Table("content", Schema = Globals.Schema.Business)]
public sealed class Content : IVersionLocal, IUpdatable
{
    [Key, Column("id")]
    public required string Id { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<ContentTranslation> Translations { get; set; } = [];
}

[Table("content_translation", Schema = Globals.Schema.Business)]
public sealed class ContentTranslation : IVersionLocal, IUpdatable
{
    [Key, Column("id")]
    public required Guid Id { get; set; }

    [Column("content_id")]
    public required string ContentId { get; set; }

    [Column("language_id")]
    public required string LanguageId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary> html </summary>
    [Column("html")]
    public string? Html { get; set; }

    /// <summary>
    /// Dictionary in format %key-name%_%language% to %url%
    /// </summary>
    [Column("json")]
    public string? Json { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
