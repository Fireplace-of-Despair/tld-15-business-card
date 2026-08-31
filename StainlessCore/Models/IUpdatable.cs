// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;

namespace StainlessCore.Models;

/// <summary> An entity that keeps the time of its creation and the time of its last change. </summary>
public interface IUpdatable
{
    /// <summary> Get created at date in UTC </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary> Get updated at date in UTC </summary>
    public DateTimeOffset UpdatedAt { get; }
}
