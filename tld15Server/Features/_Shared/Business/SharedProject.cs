// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// A project or an article, whole: what it is, when it was published, where its poster lives, the
/// links it offers and one translation per locale. The editor reads and writes this shape.
/// </summary>
public sealed class SharedProject
{
    /// <summary> The id an address carries. Lowercase latin, digits and <c>-_.</c>, chosen by hand. </summary>
    public string Id { get; set; } = string.Empty;

    public string ProjectTypeId { get; set; } = string.Empty;

    public string DivisionId { get; set; } = string.Empty;

    /// <summary> The address of the poster. Pictures are linked, never uploaded to this server. </summary>
    public string PosterUrl { get; set; } = string.Empty;

    /// <summary> The date the work was published, which the cards order and show. </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary> The links the card offers. One set for the whole project, not one per locale. </summary>
    public List<SharedLink> Links { get; set; } = [];

    public List<SharedProjectTranslation> Translations { get; set; } = [];

    /// <summary> The translation of a locale, or null when the project carries none. </summary>
    public SharedProjectTranslation? Translation(string languageId)
    {
        return Translations.Find(x => x.LanguageId == languageId);
    }
}

/// <summary> What a project reads as in one locale. </summary>
public sealed class SharedProjectTranslation
{
    public string LanguageId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary> The line under the title on a card. </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary> What the poster shows, for a reader who cannot see it. </summary>
    public string PosterAlt { get; set; } = string.Empty;

    /// <summary> The body of the work, as markdown. </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary> Whether the locale carries anything at all. An empty one is not stored. </summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title)
        && string.IsNullOrWhiteSpace(Subtitle)
        && string.IsNullOrWhiteSpace(PosterAlt)
        && string.IsNullOrWhiteSpace(Markdown);
}
