// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using StainlessCore.Exceptions;

namespace tld15ServerTests.StainlessCores.UnitTests.Exceptions;

[Trait("Category", "StainlessCore")]
public sealed class IncidentException_Tests
{
    [Fact]
    public void Constructor_DefaultIncidentCodeMustBeGeneral()
    {
        var ee = new IncidentException();

        Assert.Equal(IncidentCode.General, ee.Code);
    }

    [Fact]
    public void Constructor_DefaultStringIncidentCodeMustBeGeneral()
    {
        var ee = new IncidentException("message");

        Assert.Equal(IncidentCode.General, ee.Code);
    }

    [Fact]
    public void Constructor_DefaultStringExceptionIncidentCodeMustBeGeneral()
    {
        var ee = new IncidentException("message", new StackOverflowException());

        Assert.Equal(IncidentCode.General, ee.Code);
    }

    [Fact]
    public void Constructor_ShouldHideMessage()
    {
        var ee = new IncidentException(IncidentCode.Test);

        Assert.Equal(string.Empty, ee.Message);
    }

    [Fact]
    public void Constructor_ShouldOverdriveMessage()
    {
        var message = "test message";
        var ee = new IncidentException(IncidentCode.Test, message);

        Assert.Equal(message, ee.Message);
    }
}
