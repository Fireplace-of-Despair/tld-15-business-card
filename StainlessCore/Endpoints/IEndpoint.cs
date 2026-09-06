// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Routing;

namespace StainlessCore.Endpoints;

/// <summary> Metadata that ties an endpoint to the feature that it exposes </summary>
public record EndpointMetadata
{
    /// <summary>
    /// Get or set the identifier of the feature. The value must equal the static Id of the feature.
    /// The authorization policy of the endpoint uses this value.
    /// </summary>
    public string FeatureId { get; set; } = default!;
}

/// <summary> Endpoint for webhooks and integrations </summary>
public interface IEndpointExternal
{
    /// <summary> Get the metadata of the endpoint. </summary>
    public static abstract EndpointMetadata Metadata { get; }

    /// <summary> Map the routes of the endpoint into the route group. </summary>
    /// <param name="apiGroup">The route group of the external area.</param>
    public static abstract void ConfigureRouting(RouteGroupBuilder apiGroup);
}

/// <summary> Public endpoint </summary>
public interface IEndpointPublic
{
    /// <summary> Get the metadata of the endpoint. </summary>
    public static abstract EndpointMetadata Metadata { get; }

    /// <summary> Map the routes of the endpoint into the route group. </summary>
    /// <param name="apiGroup">The route group of the public area.</param>
    public static abstract void ConfigureRouting(RouteGroupBuilder apiGroup);
}

/// <summary> Endpoint protected by an API key </summary>
public interface IEndpointProtected
{
    /// <summary> Get the metadata of the endpoint. </summary>
    public static abstract EndpointMetadata Metadata { get; }

    /// <summary> Map the routes of the endpoint into the route group. </summary>
    /// <param name="apiGroup">The route group of the protected area.</param>
    public static abstract void ConfigureRouting(RouteGroupBuilder apiGroup);
}
