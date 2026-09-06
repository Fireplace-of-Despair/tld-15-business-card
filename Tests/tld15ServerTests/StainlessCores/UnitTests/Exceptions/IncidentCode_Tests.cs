// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
    [InlineData(IncidentCode.ValidationCoreMissmatch)]
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
