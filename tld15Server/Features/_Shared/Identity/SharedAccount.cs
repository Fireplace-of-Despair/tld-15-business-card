// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using StainlessCore.Models;

namespace tld15Server.Features.Shared.Identity;

public sealed record SharedAccount : IUpdatable, IVersionLocal
{
    public Guid? Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string StatusId { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
    public List<SharedApiKey> ApiKeys { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long VersionLocal { get; set; }
}
