// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using StainlessCore.Exceptions;

namespace tld15Server.Frontend.Components.Common;

public partial class FriendlyIncident
{
    [Inject] private IStringLocalizer<Localization.Resources> localizer { get; set; } = default!;
    [Parameter] public IncidentCode? Code { get; set; }
}

