// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
