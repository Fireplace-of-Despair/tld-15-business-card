// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;

namespace tld15Server.Frontend.Components.Common.LocalTime;

public sealed class BrowserTimeProvider : TimeProvider
{
    private TimeZoneInfo? _browserLocalTimeZone;

    public event EventHandler? LocalTimeZoneChanged;

    public override TimeZoneInfo LocalTimeZone => _browserLocalTimeZone ?? base.LocalTimeZone;

    internal bool IsLocalTimeZoneSet => _browserLocalTimeZone != null;

    public void SetBrowserTimeZone(string timeZone)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo))
        {
            timeZoneInfo = null;
        }

        if (timeZoneInfo != LocalTimeZone)
        {
            _browserLocalTimeZone = timeZoneInfo;
            LocalTimeZoneChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
