// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Collections.Generic;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// One block of the front page, in the locale it is read in: what it is called, the picture beside
/// it, the body it carries and the links it offers.
/// </summary>
/// <remarks>
/// The links belong to the content itself and not to one of its translations, the same way a project
/// keeps one set for the whole of it: a profile is the same address whichever language a reader
/// arrives in, and the language a link speaks is a badge the row carries, not the locale it lives in.
/// </remarks>
public sealed class SharedContent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string PosterAlt { get; set; } = string.Empty;
    public string? Markdown { get; set; } = null;

    /// <summary> The links of the content as they are stored: an address to the language it speaks. </summary>
    public string? LinksJson { get; set; } = null;

    /// <summary> The stored links as rows a page can draw, keyed by the address each one opens. </summary>
    public Dictionary<string, string> LinksToDictionary()
    {
        if (string.IsNullOrEmpty(LinksJson)) { return []; }

        return SharedLink.JsonToDictionary(LinksJson);
    }
}
