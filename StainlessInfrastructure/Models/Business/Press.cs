// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Business;

/// <summary> One mention of this body of work somewhere else. </summary>
[Table("press", Schema = Globals.Schema.Business)]
public sealed class Press : IVersionLocal, IUpdatable
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary> Where the mention was published. A card leads here, and off this site. </summary>
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("poster_url")]
    public string PosterUrl { get; set; } = string.Empty;

    /// <summary> When the mention appeared where it appeared, which the editor owns. </summary>
    [Column("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<PressTranslation> Translations { get; set; } = [];
}

[Table("press_translation", Schema = Globals.Schema.Business)]
public sealed class PressTranslation : IVersionLocal, IUpdatable
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("press_id")]
    public required Guid PressId { get; set; }

    [Column("language_id")]
    public string LanguageId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [Column("poster_alt")]
    public string PosterAlt { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
