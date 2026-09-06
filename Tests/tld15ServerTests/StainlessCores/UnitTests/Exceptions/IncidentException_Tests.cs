// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
