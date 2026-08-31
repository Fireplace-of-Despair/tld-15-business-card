// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;

namespace StainlessInfrastructure.Models.Reference;

[Table("account_type", Schema = Globals.Schema.Reference)]
public sealed class AccountType : IVersionLocal
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<AccountTypeTranslation> Translations { get; set; } = [];
}

[Table("account_type_translation", Schema = Globals.Schema.Reference)]
public sealed class AccountTypeTranslation : ITranslation
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("account_type_id")]
    public required string AccountTypeId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("language_id")]
    public string LanguageId { get; set; } = string.Empty;
}
