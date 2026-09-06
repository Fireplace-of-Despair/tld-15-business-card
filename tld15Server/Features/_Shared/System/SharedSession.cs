// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;

namespace tld15Server.Features.Shared.System;

public sealed record SharedSession
{
    public required Guid Id { get; set; }
    public required Guid AccountId { get; set; }
    public required string Login { get; set; }
    public required List<string> Features { get; set; } = [];
    public required string? UserAgent { get; set; }
    public required string? UserIP { get; set; }
    public required string? AcceptLanguage { get; set; }
    public required string? AcceptEncoding { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
