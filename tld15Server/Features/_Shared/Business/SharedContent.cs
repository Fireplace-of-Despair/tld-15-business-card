// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;

namespace tld15Server.Features.Shared.Business;

public sealed class SharedContent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Html { get; set; } = null;
    public string? Json { get; set; } = null;

    public Dictionary<string, string> JsonToDictionary()
    {
        return ContentJson.ToDictionary(Json);
    }
}
