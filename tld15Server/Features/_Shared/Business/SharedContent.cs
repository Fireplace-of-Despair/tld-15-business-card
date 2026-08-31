using System.Collections.Generic;
using System.Text.Json;

namespace tld15Server.Features.Shared.Business;

public sealed class SharedContent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Html { get; set; } = null;
    public string? Json { get; set; } = null;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Dictionary<string, string> JsonToDictionary()
    {
        if (string.IsNullOrEmpty(Json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(Json, jsonSerializerOptions) ?? [];
    }
}
