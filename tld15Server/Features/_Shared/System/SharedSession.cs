// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
