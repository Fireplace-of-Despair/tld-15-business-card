// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StainlessInfrastructure.Composition;

public static class ServiceInjection
{
    /// <summary> Register one context factory for each schema. </summary>
    /// <remarks> A handler takes a factory and creates one context per unit of work. </remarks>
    [RequiresUnreferencedCode("EF Core is not fully compatible with trimming. See https://aka.ms/efcore-docs-trimming for guidance.")]
    [RequiresDynamicCode("EF Core is not fully compatible with NativeAOT. See https://aka.ms/efcore-docs-trimming for guidance.")]
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContextFactory<DataContextIdentity>
        (
            options => options.UseNpgsql(connectionString)
        );
        builder.Services.AddDbContextFactory<DataContextReference>
        (
            options => options.UseNpgsql(connectionString)
        );
        builder.Services.AddDbContextFactory<DataContextBusiness>
        (
            options => options.UseNpgsql(connectionString)
        );
        builder.Services.AddDbContextFactory<DataContextArchive>
        (
            options => options.UseNpgsql(connectionString)
        );

        return builder;
    }
}
