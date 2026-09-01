// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Concurrent;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace tld15Server.Services;

/// <summary>
/// Turns the markdown a content translation stores into the html a page renders. The database holds
/// markdown and nothing else, so this is the single place that decides what markup a visitor gets.
/// </summary>
/// <remarks>
/// Nothing an editor types reaches the browser as markup of its own: the pipeline drops the html
/// parsers, so a tag in the source renders as the text of a tag, and an address keeps only a scheme
/// a browser may follow. That is what makes the result safe to hand to a MarkupString — the markup
/// comes from this renderer, never from a row of the database.
/// </remarks>
public sealed class MarkdownService
{
    /// <summary>
    /// How many rendered texts the cache holds. The site carries a handful of them, so this is a
    /// backstop against a cache that only ever grows, not a working size.
    /// </summary>
    private const int CacheLimit = 64;

    /// <summary> GitHub flavour, minus every construct that would let markup through. </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()        // | a | table |
        .UseEmphasisExtras()    // ~~strikethrough~~
        .UseTaskLists()         // - [x] done
        .UseAutoLinks()         // a bare address becomes a link
        .UseAutoIdentifiers()   // a heading takes an id, so a section can be linked to
        .DisableHtml()          // raw html is text, not markup
        .Build();

    /// <summary>
    /// The markdown itself is the key: parsing costs orders of magnitude more than hashing the
    /// source, and a text that did not change renders to the html it rendered to the last time.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    /// <summary> The html of a markdown text, or an empty string when there is nothing to render. </summary>
    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) { return string.Empty; }

        if (_cache.TryGetValue(markdown, out var cached)) { return cached; }

        var html = Render(markdown);

        if (_cache.Count >= CacheLimit) { _cache.Clear(); }

        _cache[markdown] = html;

        return html;
    }

    private string Render(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            SanitizeLink(link);
        }

        foreach (var autolink in document.Descendants<AutolinkInline>())
        {
            SanitizeAutolink(autolink);
        }

        return document.ToHtml(_pipeline);
    }

    /// <summary>
    /// Keeps a link on a scheme a browser may follow, and sends an outward link to its own tab. An
    /// address this application will not serve loses the address rather than the text: the reader
    /// still reads the words, the browser has nothing to follow.
    /// </summary>
    private static void SanitizeLink(LinkInline link)
    {
        var url = UrlPolicy.Clean(link.Url);
        var allowed = link.IsImage ? UrlPolicy.IsImageSource(url) : UrlPolicy.IsFollowable(url);

        if (!allowed)
        {
            link.Url = string.Empty;
            return;
        }

        link.Url = url;

        // An address inside the site keeps the tab it was read in. An image is not something the
        // reader follows, so neither carries the attributes of an outward link.
        if (link.IsImage || UrlPolicy.SchemeOf(url).Length == 0) { return; }

        AddOutwardAttributes(link);
    }

    /// <summary> The same rules for the &lt;address&gt; form, which the renderer writes on its own. </summary>
    private static void SanitizeAutolink(AutolinkInline autolink)
    {
        // A mail autolink carries no scheme of its own: the renderer writes the mailto itself.
        if (autolink.IsEmail) { return; }

        var url = UrlPolicy.Clean(autolink.Url);

        if (UrlPolicy.SchemeOf(url).Length == 0 || !UrlPolicy.IsFollowable(url))
        {
            autolink.Url = string.Empty;
            return;
        }

        autolink.Url = url;

        AddOutwardAttributes(autolink);
    }

    /// <summary> A link that leaves the site does not hand the page it opens a handle on this one. </summary>
    private static void AddOutwardAttributes(IMarkdownObject link)
    {
        var attributes = link.GetAttributes();

        attributes.AddPropertyIfNotExist("target", "_blank");
        attributes.AddPropertyIfNotExist("rel", "noopener noreferrer");
    }
}
