// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Models;

/// <summary> A reference to an entity. It carries the key and the display name, and no other field. </summary>
/// <typeparam name="T">Type of the key.</typeparam>
public class SharedReference<T>
{
    /// <summary> Get or set the key of the entity. </summary>
    public required T Id { get; set; }

    /// <summary> Get or set the display name of the entity. </summary>
    public required string Name { get; set; }
}
