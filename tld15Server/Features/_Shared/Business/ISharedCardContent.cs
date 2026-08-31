using System;
using System.Collections.Generic;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

public interface ISharedCardContent
{
    public string Id { get; }
    public string Title { get; }
    public string DivisionId { get; }
    public string DivisionName { get; }
    public string PosterUrl { get; }
    public string PosterAlt { get; }
    public string Subtitle { get; }
    public DateTimeOffset CreatedAt { get; }
}

public interface ISharedProjectPreview
{
    public string LinksJson { get; }

    public Dictionary<string, string> LinksToDictionary()
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(LinksJson) ?? [];
    }
}

public interface ISharedArticlePreview
{
}
