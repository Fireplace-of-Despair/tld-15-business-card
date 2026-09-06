// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
