// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;

namespace tld15Server.Features.Shared.Identity;

public sealed record SharedApiKey
{
    public Guid? Id { get; set; }

    /// <summary>
    /// The key itself, present only in the result of the request that created it.
    /// Stored hashed, so it can never be shown again.
    /// </summary>
    public string? Value { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

