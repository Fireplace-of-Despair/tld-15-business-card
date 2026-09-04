// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Models;

/// <summary>
/// Entity with per-row optimistic versioning
/// </summary>
public interface IVersionLocal
{
    /// <summary> Get/set per-row optimistic versioning </summary>
    public long VersionLocal { get; set; }
}
