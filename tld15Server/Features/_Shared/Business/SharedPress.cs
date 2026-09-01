// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// One mention of this body of work somewhere else: where it was published, when, what it was
/// called, and the picture the card carries. There is no body — the words belong to whoever wrote
/// them and are read where they were published.
/// </summary>
public sealed class SharedPress
{
    /// <summary> Null while the mention is being created. </summary>
    public Guid? Id { get; set; }

    /// <summary> The address the card leads to, which is off this site. </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary> The address of the picture. Pictures are linked, never uploaded to this server. </summary>
    public string PosterUrl { get; set; } = string.Empty;

    /// <summary> When the mention appeared where it appeared, which the cards order and show. </summary>
    public DateTimeOffset PublishedAt { get; set; }

    public List<SharedPressTranslation> Translations { get; set; } = [];

    /// <summary> The translation of a locale, or null when the mention carries none. </summary>
    public SharedPressTranslation? Translation(string languageId)
    {
        return Translations.Find(x => x.LanguageId == languageId);
    }
}

/// <summary> What a mention reads as in one locale. </summary>
public sealed class SharedPressTranslation
{
    public string LanguageId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary> The line under the title on the card: who published it, or what it said. </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary> What the picture shows, for a reader who cannot see it. </summary>
    public string PosterAlt { get; set; } = string.Empty;

    /// <summary> Whether the locale carries anything at all. An empty one is not stored. </summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title)
        && string.IsNullOrWhiteSpace(Subtitle)
        && string.IsNullOrWhiteSpace(PosterAlt);
}
