// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Models;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Reference;

namespace StainlessInfrastructure.Models.Identity;

[Table("account", Schema = Globals.Schema.Identity)]
public sealed class Account : IUpdatable, IVersionLocal
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("login")]
    public string Login { get; set; } = string.Empty;

    /// <summary> Get or set the PBKDF2 hash of the password. The server never stores the plain password. </summary>
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("salt")]
    public string Salt { get; set; } = string.Empty;

    [Column("account_status_id")]
    public string AccountStatusId { get; set; } = string.Empty;

    [Column("account_type_id")]
    public string AccountTypeId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<Feature> Features { get; set; } = [];

    public ICollection<ApiKey> ApiKeys { get; set; } = [];

    public AccountStatus? AccountStatus { get; set; }

    public AccountType? AccountType { get; set; }

}

[Table("feature", Schema = Globals.Schema.Identity)]
public sealed class Feature : IVersionLocal
{
    [Key, Column("id")]
    public required string Id { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public ICollection<Account> Accounts { get; set; } = [];

    public ICollection<AccountToFeature> AccountToFeatures { get; } = [];

    public ICollection<FeatureTranslation> Translations { get; set; } = [];
}

[Table("feature_translation", Schema = Globals.Schema.Identity)]
public sealed class FeatureTranslation : ITranslation
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("feature_id")]
    public required string FeatureId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("language_id")]
    public string LanguageId { get; set; } = string.Empty;
}

[Table("account_to_feature", Schema = Globals.Schema.Identity)]
[PrimaryKey(nameof(AccountId), nameof(FeatureId))]
public sealed class AccountToFeature
{
    [Key, Column("account_id")]
    public required Guid AccountId { get; set; }

    [Key, Column("feature_id")]
    public required string FeatureId { get; set; }

    public Account? Account { get; set; }

    public Feature? Feature { get; set; }
}

/// <summary> An API key of an account. The table holds the hash of the key, never the key itself. </summary>
[Table("api_key", Schema = Globals.Schema.Identity)]
public sealed class ApiKey : IUpdatable, IVersionLocal
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    [Column("account_id")]
    public required Guid AccountId { get; set; }

    [Column("key_hash")]
    public required string KeyHash { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary> Get or set the time of the last use, in UTC. The value stays empty until the first use. </summary>
    [Column("last_used_at")]
    public DateTimeOffset? LastUsedAt { get; set; }

    [Column("version_local")]
    public long VersionLocal { get; set; }

    public Account? Account { get; set; }
}
