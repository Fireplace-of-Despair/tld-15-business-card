// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;

namespace StainlessCore.Models;

/// <summary> An item together with a count of the rows that belong to it </summary>
/// <typeparam name="T1">Type of the item.</typeparam>
/// <param name="item">The item that the count belongs to.</param>
/// <param name="count">Number of rows that belong to the item.</param>
[Serializable]
public class ItemCount<T1>(T1 item, int count)
{
    private readonly T1 _item = item;
    private readonly int _count = count;

    /// <summary> Get the item. </summary>
    public T1 Item => _item;

    /// <summary> Get the count of the rows that belong to the item. </summary>
    public int Count => _count;

    /// <summary> Get the item and the count as one line of text. </summary>
    /// <returns>The item, then the count in square brackets.</returns>
    public override string ToString()
    {
        return $"{_item} [{_count}]";
    }
}
