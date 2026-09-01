// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// The json contract of a stored set of links: a dictionary of a link key to a url. A content
/// translation and a project both keep their links this way, and reading and writing share this one
/// place, so an editor cannot store a shape the pages fail to read.
/// </summary>
public static class LinkJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary> Read the stored dictionary, or an empty one when nothing holds any links. </summary>
    public static Dictionary<string, string> ToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _options) ?? [];
    }

    /// <summary>
    /// Write the dictionary back, or null when it holds nothing: a page skips a content whose json
    /// is empty, and null says "no links" without a stored "{}" that looks like data.
    /// </summary>
    public static string? ToJson(Dictionary<string, string> links)
    {
        if (links.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(links, _options);
    }
}
