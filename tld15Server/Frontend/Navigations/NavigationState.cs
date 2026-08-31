// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;

namespace tld15Server.Frontend.Components.Navigations;

public class NavigationState
{
    public event Action? OnChange;

    public void Changed()
    {
        OnChange?.Invoke();
    }
}
