// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Reference;

namespace StainlessInfrastructure;

[RequiresUnreferencedCode("EF Core is not fully compatible with trimming. See https://aka.ms/efcore-docs-trimming for guidance.")]
[RequiresDynamicCode("EF Core is not fully compatible with NativeAOT. See https://aka.ms/efcore-docs-trimming for guidance.")]
public class DataContextReference : DbContext
{
    public virtual DbSet<Language> Languages => Set<Language>();
    public virtual DbSet<AccountStatus> AccountStatuses => Set<AccountStatus>();
    public virtual DbSet<AccountStatusTranslation> AccountStatusTranslations => Set<AccountStatusTranslation>();
    public virtual DbSet<AccountType> AccountTypes => Set<AccountType>();
    public virtual DbSet<AccountTypeTranslation> AccountTypeTranslations => Set<AccountTypeTranslation>();

    public virtual DbSet<Division> Divisions => Set<Division>();
    public virtual DbSet<DivisionTranslation> DivisionTranslations => Set<DivisionTranslation>();

    public virtual DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
    public virtual DbSet<ProjectTypeTranslation> ProjectTypeTranslations => Set<ProjectTypeTranslation>();

    public DataContextReference() { }
    public DataContextReference(DbContextOptions<DataContextReference> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Globals.Schema.Reference);
    }
}
