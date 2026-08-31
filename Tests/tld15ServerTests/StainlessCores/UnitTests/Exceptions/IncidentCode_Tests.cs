// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using StainlessCore.Exceptions;

namespace tld15ServerTests.StainlessCores.UnitTests.Exceptions;

[Trait("Category", "StainlessCore")]
public class IncidentCode_Tests
{
    [Theory]
    [InlineData(IncidentCode.Unauthorized, 401)]
    [InlineData(IncidentCode.Forbidden, 403)]
    [InlineData(IncidentCode.WrongPassword, 401)]
    [InlineData(IncidentCode.NotFound, 404)]
    [InlineData(IncidentCode.VersionMismatch, 409)]
    public void ToHTTPCode_ReturnsExpectedHttpCode_ForMappedIncidentCodes(
        IncidentCode incidentCode,
        int expectedHttpCode)
    {
        var result = incidentCode.ToHTTPCode();

        Assert.Equal(expectedHttpCode, result);
    }

    [Theory]
    [InlineData(IncidentCode.Test)]
    [InlineData(IncidentCode.Fatal)]
    [InlineData(IncidentCode.General)]
    [InlineData(IncidentCode.Validation)]
    public void ToHTTPCode_Returns500_ForUnmappedIncidentCodes(
        IncidentCode incidentCode)
    {
        var result = incidentCode.ToHTTPCode();

        Assert.Equal(500, result);
    }

    [Fact]
    public void ToHTTPCode_Returns500_ForUnknownEnumValue()
    {
        var unknownCode = (IncidentCode)999999;

        var result = unknownCode.ToHTTPCode();

        Assert.Equal(500, result);
    }
}
