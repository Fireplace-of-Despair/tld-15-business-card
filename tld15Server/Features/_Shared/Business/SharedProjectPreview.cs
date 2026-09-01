using System;
using System.Collections.Generic;

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

    /// <summary> The date the work was published. The cards order and show this one. </summary>
    public DateTimeOffset PublishedAt { get; set; }

    public Dictionary<string, string> LinksToDictionary()
    {
        if (string.IsNullOrEmpty(LinksJson)) { return []; }

        return LinkJson.ToDictionary(LinksJson);
    }
}
