using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Reference;

namespace StainlessInfrastructure.Models.Business;

[Table("article", Schema = Globals.Schema.Business)]
public sealed class Article : IVersionLocal, IUpdatable
{
    [Key, Column("id"), StringLength(64)]
    public required string Id { get; set; }

    [Column("division_id"), Required, StringLength(64)]
    public string DivisionId { get; set; } = string.Empty;

    [Column("poster_url"), StringLength(512)]
    public string PosterUrl { get; set; } = string.Empty;
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    [ForeignKey(nameof(DivisionId))]
    public Division Division { get; set; } = null!;

    public ICollection<ArticleTranslation> Translations { get; set; } = [];
}

[Table("article_translation", Schema = Globals.Schema.Business)]
public sealed class ArticleTranslation : IVersionLocal, IUpdatable
{
    [Key, Column("id")]
    public required Guid Id { get; set; }

    [Column("article_id"), Required, StringLength(64)]
    public required string ArticleId { get; set; }

    [Column("language_id"), Required, StringLength(8)]
    public required string LanguageId { get; set; }

    [Column("poster_alt")]
    [StringLength(256)]
    public string PosterAlt { get; set; } = string.Empty;

    [Column("title"), StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [Column("subtitle"), StringLength(256)]
    public string Subtitle { get; set; } = string.Empty;

    [Column("content_html")]
    public string ContentHtml { get; set; } = string.Empty;
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
