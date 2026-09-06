// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using StainlessCore.Endpoints;
using tld15Server.Features.System;

namespace tld15Server.Endpoints.Public.System;

public sealed class GetPingEndpoint : IEndpointPublic
{
    public static EndpointMetadata Metadata => new() { FeatureId = PingGetFeature.Id };

    public static void ConfigureRouting(RouteGroupBuilder apiGroup)
    {
        apiGroup.MapGet("/ping",
            async ([FromServices] IMediator mediator) =>
            {
                return await mediator.Send(new PingGetFeature.Query());
            })
            .WithMetadata(Metadata);
    }
}
