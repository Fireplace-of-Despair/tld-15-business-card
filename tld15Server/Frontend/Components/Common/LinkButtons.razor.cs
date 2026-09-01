// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Components.Common;

/// <summary>
/// A row of the links a work offers: the icon of each, badged with the language it speaks. The
/// buttons carry no spacing of their own, so the row around them decides how they sit.
/// </summary>
public partial class LinkButtons
{
    [Parameter] public IReadOnlyList<SharedLink> Links { get; set; } = [];
}
