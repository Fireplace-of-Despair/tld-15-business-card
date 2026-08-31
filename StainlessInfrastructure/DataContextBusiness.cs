// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Business;

namespace StainlessInfrastructure;

[RequiresUnreferencedCode("EF Core is not fully compatible with trimming. See https://aka.ms/efcore-docs-trimming for guidance.")]
[RequiresDynamicCode("EF Core is not fully compatible with NativeAOT. See https://aka.ms/efcore-docs-trimming for guidance.")]
public class DataContextBusiness : DbContext
{
    public virtual DbSet<Article> Articles => Set<Article>();
    public virtual DbSet<Content> Contents => Set<Content>();
    public virtual DbSet<Project> Projects => Set<Project>();
    public DataContextBusiness() { }
    public DataContextBusiness(DbContextOptions<DataContextBusiness> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Globals.Schema.Business);
    }
}
