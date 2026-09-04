// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Globalization;

namespace StainlessCore.Common.Helpers;

/// <summary> <see cref="string"/> helper </summary>
public static class StringHelper
{
    /// <summary> Convert <see cref="string"/> to <see cref="Guid"/> </summary>
    /// <param name="text">Text value</param>
    /// <returns><see cref="Guid"/>, or <see langword="null"/> when the text does not parse</returns>
    public static Guid? ToGuid(this string? text)
    {
        return Guid.TryParse(text, out var guid) ? guid : null;
    }

    /// <summary> Convert <see cref="string"/> to <see cref="int"/> </summary>
    /// <param name="text">Text value</param>
    /// <param name="fallback">Fallback if string is not <see cref="int"/> </param>
    /// <returns><see cref="int"/>, or <paramref name="fallback"/> when the text does not parse</returns>
    public static int ToInt(this string? text, int fallback = 0)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }
}
