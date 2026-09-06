// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
