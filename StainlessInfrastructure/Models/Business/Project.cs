using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Reference;

namespace StainlessInfrastructure.Models.Business;

[Table("project", Schema = Globals.Schema.Business)]
public sealed class Project : IVersionLocal, IUpdatable
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("division_id")]
    public string DivisionId { get; set; } = string.Empty;

    [Column("project_type_id")]
    public string ProjectTypeId { get; set; } = string.Empty;

    [Column("poster_url")]
    public string PosterUrl { get; set; } = string.Empty;

    [Column("links_json")]
    public string? LinksJson { get; set; }

    [Column("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    [ForeignKey(nameof(DivisionId))]
    public Division Division { get; set; } = null!;

    [ForeignKey(nameof(ProjectTypeId))]
    public ProjectType ProjectType { get; set; } = null!;

    public ICollection<ProjectTranslation> Translations { get; set; } = [];
}

[Table("project_translation", Schema = Globals.Schema.Business)]
public sealed class ProjectTranslation : IVersionLocal, IUpdatable
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("project_id")]
    public required string ProjectId { get; set; }

    [Column("language_id")]
    public string LanguageId { get; set; } = string.Empty;

    [Column("poster_alt")]
    public string PosterAlt { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary> The body of the translation, as the markdown an editor typed. </summary>
    [Column("markdown")]
    public string? Markdown { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
