// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Models;

/// <summary>
/// Entity with a global monotonically increasing cross-entity version
/// </summary>
public interface IVersionGlobal
{
    /// <summary> Get the global version monotonically increasing cross-entity </summary>
    public long VersionGlobal { get; }
}
