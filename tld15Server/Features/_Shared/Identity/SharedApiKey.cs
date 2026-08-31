// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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

