// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Collections.Generic;

namespace StainlessCore.Exceptions;

/// <summary>
/// Obfuscated code of an exceptional situation. The server sends this code to a client in place of the
/// real exception. A client branches only on a specific code.
/// </summary>
public enum IncidentCode
{
    /// <summary> Reserved for the test suite. No production code throws this code. </summary>
    Test = 0,

    /// <summary> Reserved for an unrecoverable failure. No production code throws this code. </summary>
    Fatal = 1,

    /// <summary> The fallback code. A generic exception becomes this code. </summary>
    General = 1_000,

    /// <summary> The caller sent no API key, or the key does not match an account. </summary>
    Unauthorized = 1_401,

    /// <summary> The caller is known, but the account does not own the feature. </summary>
    Forbidden = 1_403,

    /// <summary> The request names an entity that does not exist. </summary>
    NotFound = 1_404,

    /// <summary> The caller passed the sign-in attempt limit. </summary>
    TooManyAttempts = 1_429,

    /// <summary> The version of the request differs from the version of the stored entity. </summary>
    VersionMismatch = 1_409,

    /// <summary> The password does not match the stored hash. </summary>
    WrongPassword = 2_000,

    /// <summary> General validation error</summary>
    Validation = 10_000,

    /// <summary> The request carries a value that a feature refuses to store. </summary>
    ValidationCoreMissmatch = 10_001,
}

/// <summary> Maps an <see cref="IncidentCode"/> onto an HTTP status code. </summary>
public static class IncidentCodeExtension
{
    private static readonly Dictionary<IncidentCode, int> _incidentToHttpCode = new()
    {
        { IncidentCode.Unauthorized, 401 },

        { IncidentCode.Forbidden, 403 },
        { IncidentCode.WrongPassword, 401 },

        { IncidentCode.NotFound, 404 },

        { IncidentCode.VersionMismatch, 409 },

        { IncidentCode.TooManyAttempts, 429 },
    };

    /// <summary> Get the HTTP status code for an incident code. </summary>
    /// <param name="code">The incident code to map.</param>
    /// <returns>The mapped status code, or 500 when the map holds no entry for the code.</returns>
    public static int ToHTTPCode(this IncidentCode code)
    {
        if (_incidentToHttpCode.TryGetValue(code, out var value))
        {
            return value;
        }

        return 500;
    }
}
