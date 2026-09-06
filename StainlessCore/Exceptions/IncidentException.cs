// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;

namespace StainlessCore.Exceptions;

/// <summary> Exceptional situation <see cref="Exception"/> described with a <see cref="IncidentCode"/> </summary>
public sealed class IncidentException : Exception
{
    /// <summary> Code of a exceptional situation </summary>
    public IncidentCode Code { get; }

    /// <summary> Create an exceptional situation <see cref="Exception"/> </summary>
    /// <param name="code"> Code of an exceptional situation </param>
    public IncidentException(IncidentCode code) : base(string.Empty)
    {
        Code = code;
    }

    /// <summary> Create an exceptional situation <see cref="Exception"/> </summary>
    /// <param name="code"> Code of an exceptional situation </param>
    /// <param name="publicMessage"> Extra message, that will be included into the public response </param>
    public IncidentException(IncidentCode code, string publicMessage) : base(publicMessage)
    {
        Code = code;
    }

    /// <summary> Create an exceptional situation <see cref="Exception"/> with no message </summary>
    /// <remarks> The code becomes <see cref="IncidentCode.General"/>. </remarks>
    public IncidentException() : base()
    {
        Code = IncidentCode.General;
    }

    /// <summary> Create an exceptional situation <see cref="Exception"/> from a message </summary>
    /// <param name="publicMessage"> Extra message, that will be included into the public response </param>
    /// <remarks> The code becomes <see cref="IncidentCode.General"/>. </remarks>
    public IncidentException(string? publicMessage) : base(publicMessage)
    {
        Code = IncidentCode.General;
    }

    /// <summary> Create an exceptional situation <see cref="Exception"/> that wraps another one </summary>
    /// <param name="publicMessage"> Extra message, that will be included into the public response </param>
    /// <param name="innerException"> The exception that caused this one </param>
    /// <remarks> The code becomes <see cref="IncidentCode.General"/>. </remarks>
    public IncidentException(string? publicMessage, Exception? innerException) : base(publicMessage, innerException)
    {
        Code = IncidentCode.General;
    }
}
