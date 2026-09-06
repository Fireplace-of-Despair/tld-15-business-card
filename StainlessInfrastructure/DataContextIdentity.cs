// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Identity;

namespace StainlessInfrastructure;

[RequiresUnreferencedCode("EF Core is not fully compatible with trimming. See https://aka.ms/efcore-docs-trimming for guidance.")]
[RequiresDynamicCode("EF Core is not fully compatible with NativeAOT. See https://aka.ms/efcore-docs-trimming for guidance.")]
public class DataContextIdentity : DbContext
{
    public virtual DbSet<Account> Accounts => Set<Account>();
    public virtual DbSet<Feature> Features => Set<Feature>();
    public virtual DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public virtual DbSet<AccountToFeature> AccountToFeatures => Set<AccountToFeature>();
    public virtual DbSet<FeatureTranslation> FeatureTranslations => Set<FeatureTranslation>();

    public DataContextIdentity() { }
    public DataContextIdentity(DbContextOptions<DataContextIdentity> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Globals.Schema.Identity);

        modelBuilder.Entity<AccountToFeature>()
            .HasKey(sc => new { sc.AccountId, sc.FeatureId });

        modelBuilder.Entity<Account>()
            .HasMany(e => e.Features)
            .WithMany(e => e.Accounts)
            .UsingEntity<AccountToFeature>();
        modelBuilder.Entity<Account>()
            .HasMany(a => a.ApiKeys)
            .WithOne(k => k.Account)
            .HasForeignKey(k => k.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
