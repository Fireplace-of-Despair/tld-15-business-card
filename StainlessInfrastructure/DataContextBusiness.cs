// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StainlessInfrastructure.Composition;
using StainlessInfrastructure.Models.Business;

namespace StainlessInfrastructure;

[RequiresUnreferencedCode("EF Core is not fully compatible with trimming. See https://aka.ms/efcore-docs-trimming for guidance.")]
[RequiresDynamicCode("EF Core is not fully compatible with NativeAOT. See https://aka.ms/efcore-docs-trimming for guidance.")]
public class DataContextBusiness : DbContext
{
    public virtual DbSet<Content> Contents => Set<Content>();
    public virtual DbSet<Project> Projects => Set<Project>();
    public virtual DbSet<Press> Presses => Set<Press>();
    public DataContextBusiness() { }
    public DataContextBusiness(DbContextOptions<DataContextBusiness> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Globals.Schema.Business);
    }
}
