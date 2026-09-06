// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using StainlessCore.Exceptions;

namespace StainlessCore.Endpoints;

/// <summary>
/// Endpoint filter that catches every exception of an endpoint. It logs the real error and returns an
/// obfuscated <see cref="Incident"/> to the client.
/// </summary>
public sealed class ExceptionHandlingFilter : IEndpointFilter
{
    /// <summary> Envelope that carries either the result of the endpoint or an incident </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    public sealed record Result<T>
    {
        /// <summary> Result </summary>
        [JsonPropertyName("result"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Item { get; internal set; }

        /// <summary> Incident or <see langword="null"/> </summary>
        [JsonPropertyName("incident"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Incident? Incident { get; internal set; }

    }

    /// <summary> Run the endpoint and wrap the outcome into a <see cref="Result{T}"/>. </summary>
    /// <param name="context">Context of the endpoint invocation.</param>
    /// <param name="next">The next step in the filter pipeline.</param>
    /// <returns>The envelope with the result, or with the incident.</returns>
    /// <remarks>
    /// An <see cref="IncidentException"/> keeps its own code. Every other exception becomes
    /// <see cref="IncidentCode.General"/>. The real exception never reaches the client.
    /// </remarks>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var execution = new Result<object>();

        try
        {
            execution.Item = await next(context);
        }
        catch (IncidentException ee)
        {
            Log.Error(ee, "An incident occurred during processing. {IncidentCode}", ee.Code);

            context.HttpContext.Response.StatusCode = ee.Code.ToHTTPCode();
            execution.Incident = new Incident(ee.Code);
        }
        catch (Exception ee)
        {
            Log.Error(ee, "An exception occurred during processing.");

            context.HttpContext.Response.StatusCode = IncidentCode.General.ToHTTPCode();
            execution.Incident = new Incident(IncidentCode.General);
        }

        context.HttpContext.Response.ContentType = "application/json";
        return execution;
    }
}
