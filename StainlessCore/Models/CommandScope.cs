// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Models;

/// <summary> The set of rows that a command reads or writes. </summary>
public enum CommandScope
{
    /// <summary> The command works on the live rows. </summary>
    Normal = 0,

    /// <summary> The command works on the archived rows. </summary>
    Archive = 1
}
