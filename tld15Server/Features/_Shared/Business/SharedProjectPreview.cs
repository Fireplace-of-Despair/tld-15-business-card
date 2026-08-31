using System;
using System.Collections.Generic;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

public class SharedProjectPreview
{
    public string Id { get; set; } = string.Empty;
    public string ProjectTypeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DivisionId { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string PosterAlt { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? LinksJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Dictionary<string, string> LinksToDictionary()
    {
        if (string.IsNullOrEmpty(LinksJson)) { return []; }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(LinksJson) ?? [];
    }
}
