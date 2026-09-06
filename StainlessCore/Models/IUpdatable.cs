// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
