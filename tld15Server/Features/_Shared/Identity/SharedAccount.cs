// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

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
