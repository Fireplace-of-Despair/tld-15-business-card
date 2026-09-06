// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
