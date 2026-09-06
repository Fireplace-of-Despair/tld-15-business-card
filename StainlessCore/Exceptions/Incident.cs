// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Text.Json.Serialization;

namespace StainlessCore.Exceptions;

/// <summary> Public representation of an exceptional situation </summary>
/// <param name="error">The code that the client receives.</param>
public sealed record Incident(IncidentCode error)
{
    /// <summary> Get the code of the exceptional situation. </summary>
    [JsonPropertyName("code")]
    public IncidentCode Code { get; private set; } = error;

    /// <summary> Get the name of the code. This text carries no detail about the real error. </summary>
    [JsonPropertyName("description")]
    public string Description => Code.ToString();
}
