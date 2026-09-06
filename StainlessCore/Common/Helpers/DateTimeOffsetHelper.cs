// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;

namespace StainlessCore.Common.Helpers;

/// <summary> <see cref="DateTimeOffset"/> helper </summary>
public static class DateTimeOffsetHelper
{
    /// <summary> Convert a UTC value into the local time of a <see cref="TimeProvider"/>. </summary>
    /// <param name="dateTime">The value to convert. The helper reads its UTC part.</param>
    /// <param name="timeProvider">The provider that names the target time zone.</param>
    /// <returns>The local value, with <see cref="DateTimeKind.Local"/>.</returns>
    public static DateTime ToTimeProviderDateTime(this DateTimeOffset dateTime, TimeProvider timeProvider)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(dateTime.UtcDateTime, timeProvider.LocalTimeZone);
        return DateTime.SpecifyKind(local, DateTimeKind.Local);
    }
}
