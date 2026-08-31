// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using StainlessCore.Exceptions;

namespace tld15Server.Frontend.Components.Common;

public partial class FriendlyIncident
{
    [Inject] private IStringLocalizer<Localization.Resources> localizer { get; set; } = default!;
    [Parameter] public IncidentCode? Code { get; set; }
}

