// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
