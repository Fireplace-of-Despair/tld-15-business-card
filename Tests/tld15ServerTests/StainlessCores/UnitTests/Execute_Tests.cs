// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Threading.Tasks;
using StainlessCore;
using StainlessCore.Exceptions;

namespace tld15ServerTests.StainlessCores.UnitTests;

[Trait("Category", "StainlessCore")]
public class Execute_Tests
{
    [Fact]
    public async Task Run_ReturnsSuccessResult_WhenFunctionCompletesSuccessfully()
    {
        const string expected = "success";

        var result = await Execute.Run(async () =>
        {
            await Task.Delay(0);
            return expected;
        });

        Assert.NotNull(result);
        Assert.Equal(expected, result.Data);
        Assert.Null(result.IncidentCode);
    }

    [Fact]
    public async Task Run_ReturnsFailureResult_WhenIncidentExceptionIsThrown()
    {
        var expectedIncident = IncidentCode.General;

        var result = await Execute.Run<string>(() =>
            throw new IncidentException(expectedIncident));

        Assert.NotNull(result);
        Assert.Null(result.Data);
        Assert.Equal(expectedIncident, result.IncidentCode);
    }

    [Fact]
    public async Task Run_ReturnsGeneralFailure_WhenUnhandledExceptionIsThrown()
    {
        var result = await Execute.Run<string>(() =>
            throw new InvalidOperationException("Unexpected failure"));

        Assert.NotNull(result);
        Assert.Null(result.Data);
        Assert.Equal(IncidentCode.General, result.IncidentCode);
    }

    [Fact]
    public async Task Run_HandlesNullReturnValue_ForReferenceType()
    {
        var result = await Execute.Run<string?>(() =>
            Task.FromResult<string?>(null));

        Assert.Null(result.Data);
        Assert.Null(result.IncidentCode);
    }
}
