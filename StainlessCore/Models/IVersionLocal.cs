// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

namespace StainlessCore.Models;

/// <summary>
/// Entity with per-row optimistic versioning
/// </summary>
public interface IVersionLocal
{
    /// <summary> Get/set per-row optimistic versioning </summary>
    public long VersionLocal { get; set; }
}
