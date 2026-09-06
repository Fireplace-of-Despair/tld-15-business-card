// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using StainlessCore.Endpoints;
using StainlessCore.Exceptions;

namespace tld15ServerTests.StainlessCores.UnitTests.Endpoints;

public class ExceptionHandlingFilter_Tests
{

    private readonly ExceptionHandlingFilter _filter = new();

    private static EndpointFilterInvocationContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();

        return new TestEndpointFilterInvocationContext(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsExecutionResult_WhenNoExceptionOccurs()
    {
        var context = CreateContext();

        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>("success");

        var result = await _filter.InvokeAsync(context, next);

        var execution = Assert.IsType<ExceptionHandlingFilter.Result<object>>(result);

        Assert.Equal("success", execution.Item);
        Assert.Null(execution.Incident);

        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsIncident_WhenIncidentExceptionIsThrown()
    {
        var context = CreateContext();

        EndpointFilterDelegate next = _ =>
            throw new IncidentException(IncidentCode.NotFound);

        var result = await _filter.InvokeAsync(context, next);

        var execution = Assert.IsType<ExceptionHandlingFilter.Result<object>>(result);

        Assert.Null(execution.Item);
        Assert.NotNull(execution.Incident);
        Assert.Equal(IncidentCode.NotFound, execution.Incident.Code);
        Assert.Equal(IncidentCode.NotFound.ToString(), execution.Incident.Description);

        Assert.Equal(404, context.HttpContext.Response.StatusCode);
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsGeneralIncident_WhenUnhandledExceptionIsThrown()
    {
        var context = CreateContext();

        EndpointFilterDelegate next = _ =>
            throw new InvalidOperationException("Unexpected error");

        var result = await _filter.InvokeAsync(context, next);

        var execution = Assert.IsType<ExceptionHandlingFilter.Result<object>>(result);

        Assert.Null(execution.Item);
        Assert.NotNull(execution.Incident);
        Assert.Equal(IncidentCode.General, execution.Incident.Code);
        Assert.Equal(IncidentCode.General.ToString(), execution.Incident.Description);

        Assert.Equal(500, context.HttpContext.Response.StatusCode);
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_SetsContentType_EvenWhenExceptionOccurs()
    {
        var context = CreateContext();

        EndpointFilterDelegate next = _ =>
            throw new Exception("Failure");

        await _filter.InvokeAsync(context, next);

        Assert.Equal("application/json", context.HttpContext.Response.ContentType);
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; } = new List<object?>();

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }

}
