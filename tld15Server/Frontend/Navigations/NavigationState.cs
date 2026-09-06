// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
