// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
