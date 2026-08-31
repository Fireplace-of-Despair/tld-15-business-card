using System;

namespace tld15Server.Features.Shared.Business;

public class SharedArticlePreview : ISharedCardContent, ISharedArticlePreview
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DivisionId { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string PosterAlt { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
