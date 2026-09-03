using System;
using System.Collections.Generic;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// What a card shows. A work of this site fills it and leads to its own page; a mention in the
/// press fills the same card, carries no division and leads off the site entirely.
/// </summary>
public class SharedCardPreview
{
    public string Id { get; set; } = string.Empty;

    /// <summary> Empty for anything that is not a work of this site. </summary>
    public string ProjectTypeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary> Empty when nothing owns the card: the badge is then not drawn. </summary>
    public string DivisionId { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string PosterAlt { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? LinksJson { get; set; }

    /// <summary> The date the work was published. The cards order and show this one. </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Where the card leads when it leads off this site. Left null, the card leads to the page of
    /// the work it stands for.
    /// </summary>
    public string? ExternalUrl { get; set; }

    public Dictionary<string, string> LinksToDictionary()
    {
        if (string.IsNullOrEmpty(LinksJson)) { return []; }

        return SharedLink.JsonToDictionary(LinksJson);
    }
}
