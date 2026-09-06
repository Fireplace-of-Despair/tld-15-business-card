// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Endpoints;
using StainlessCore.Exceptions;
using StainlessCore.Service;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Services;

namespace tld15Server.Endpoints;

public sealed class ApiKeyFilter(
      CacheManager cacheManager
    , ApiKeyService apiKeyService
    , IDbContextFactory<DataContextIdentity> contextIdentityFactory) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(Globals.ApiKey.Name, out var apiKey))
        {
            throw new IncidentException(IncidentCode.Unauthorized);
        }

        // Only the hash is stored and cached, so the presented key is hashed before every lookup.
        var apiKeyHash = apiKeyService.Hash(apiKey.ToString());

        var features = cacheManager.GetFeaturesByApiKey(apiKeyHash);
        if (features.Count == 0)
        {
            throw new IncidentException(IncidentCode.Unauthorized);
        }

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            throw new IncidentException(IncidentCode.NotFound);
        }

        var permission = endpoint.Metadata.GetMetadata<EndpointMetadata>();
        if (permission == null)
        {
            throw new IncidentException(IncidentCode.Unauthorized);
        }

        if (!features.Contains(permission.FeatureId))
        {
            throw new IncidentException(IncidentCode.Forbidden);
        }

        await TouchLastUsedAsync(apiKeyHash, context.HttpContext.RequestAborted);

        return await next(context);
    }

    internal async Task TouchLastUsedAsync(string apiKeyHash, CancellationToken ctn)
    {
        using var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn);

        await contextIdentity.ApiKeys
            .Where(x => x.KeyHash == apiKeyHash)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.LastUsedAt, DateTimeOffset.UtcNow), ctn);
    }
}
