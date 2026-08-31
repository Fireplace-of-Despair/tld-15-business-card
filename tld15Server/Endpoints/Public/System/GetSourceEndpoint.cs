// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using StainlessCore.Endpoints;
using tld15Server.Features.System;

namespace tld15Server.Endpoints.Public.System;

public sealed class GetSourceEndpoint : IEndpointPublic
{
    public static EndpointMetadata Metadata => new() { FeatureId = SourceGetFeature.Id };

    public static void ConfigureRouting(RouteGroupBuilder apiGroup)
    {
        apiGroup.MapGet("/source",
            async ([FromServices] IMediator mediator) =>
            {
                return await mediator.Send(new SourceGetFeature.Query());
            })
            .WithMetadata(Metadata);
    }
}
