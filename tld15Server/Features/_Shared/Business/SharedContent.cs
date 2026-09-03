// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Collections.Generic;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

public sealed class SharedContent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string PosterAlt { get; set; } = string.Empty;
    public string? Markdown { get; set; } = null;
    public string? Json { get; set; } = null;


    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Dictionary<string, string> JsonToDictionary()
    {
        return JsonToDictionary(Json);
    }

    public static Dictionary<string, string> JsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json)) { return []; }


        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _options) ?? [];
    }

    public static string? DictionaryToJson(Dictionary<string, string> links)
    {
        if (links.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(links, _options);
    }
}
