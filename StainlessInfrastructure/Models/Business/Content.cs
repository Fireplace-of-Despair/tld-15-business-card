// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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

    [Column("poster_url")]
    public string? PosterUrl { get; set; }

    [Column("links_json")]
    public string? LinksJson { get; set; }

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
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("content_id")]
    public required string ContentId { get; set; }

    [Column("language_id")]
    public required string LanguageId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("poster_alt")]
    public string? PosterAlt { get; set; }

    [Column("markdown")]
    public string? Markdown { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }
}
