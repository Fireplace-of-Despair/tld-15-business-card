// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
