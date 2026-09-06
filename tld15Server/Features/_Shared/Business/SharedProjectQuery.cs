// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Common.Helpers;
using StainlessCore.Exceptions;
using StainlessInfrastructure;
using StainlessInfrastructure.Models.Business;
using tld15Server.Composition;

namespace tld15Server.Features.Shared.Business;

/// <summary>
/// The read every page that shows a wall of cards makes. The front page and the archive differ in
/// which works they ask for and in nothing else, so the columns, the order and the mapping live
/// here rather than once per page.
/// </summary>
internal static class SharedProjectQuery
{
    internal sealed record TranslationRow(string LanguageId, string Title, string Subtitle, string PosterAlt);

    internal sealed record Row(
        string Id,
        string ProjectTypeId,
        string DivisionId,
        string PosterUrl,
        string? LinksJson,
        DateTimeOffset PublishedAt,
        List<TranslationRow> Translations);

    /// <summary> The two lists a wall of cards is drawn from, newest first. </summary>
    internal sealed record Cards
    {
        public List<SharedCardPreview> Articles { get; init; } = [];
        public List<SharedCardPreview> Projects { get; init; } = [];
    }

    /// <summary>
    /// The columns a card needs, newest first. The body of a work stays out on purpose: a card
    /// carries a title, a subtitle and a poster, and pulling the body of every work would dominate
    /// the payload of the page. Two locales leave the database — the requested one and the fallback
    /// a card falls back to when the requested one holds no row.
    /// </summary>
    /// <remarks>
    /// One collection leaves the database here, and the names of the divisions are read separately
    /// by <see cref="DivisionNames"/>. Two collections in one projection are joined into a single
    /// result set, so every work would come back once per division name it matched — the rows
    /// multiply instead of adding up, and EF Core says so out loud at compile time. The second read
    /// also costs less on its own terms: a division carries the same name for every work filed
    /// under it, and this way that name crosses the wire once for the page rather than once per
    /// card.
    /// </remarks>
    internal static IQueryable<Row> SelectCards(this IQueryable<Project> projects, string language, string fallback)
    {
        var types = new[] { Globals.ProjectType.Project, Globals.ProjectType.Article };

        return projects
            .Where(x => types.Contains(x.ProjectTypeId))
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new Row(
                x.Id,
                x.ProjectTypeId,
                x.DivisionId,
                x.PosterUrl,
                x.LinksJson,
                x.PublishedAt,
                x.Translations
                    .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                    .Select(tr => new TranslationRow(tr.LanguageId, tr.Title, tr.Subtitle, tr.PosterAlt))
                    .ToList()));
    }

    /// <summary>
    /// The divisions, as an id against the name it carries in this locale. The reference tables hold
    /// a handful of rows, so a wall of any size reads them in one short query.
    /// </summary>
    internal static async Task<Dictionary<string, string>> DivisionNames(
          IDbContextFactory<DataContextReference> factory
        , string language
        , CancellationToken ctn)
    {
        await using (var context = await factory.CreateDbContextAsync(ctn))
        {
            var divisions = await context
                .Divisions
                .Select(x => new
                {
                    x.Id,
                    Names = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                })
                .AsNoTracking()
                .ToListAsync(ctn);

            return divisions.ToDictionary(x => x.Id, x => x.Names.GetName(language), StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Splits the rows by what they are. The database already ordered them, so a single pass keeps
    /// both lists newest first.
    /// </summary>
    internal static Cards Split(List<Row> rows, Dictionary<string, string> divisions, string language)
    {
        var cards = new Cards();

        foreach (var row in rows)
        {
            if (row.ProjectTypeId == Globals.ProjectType.Article)
            {
                cards.Articles.Add(ToPreview(row, divisions, language));
                continue;
            }

            if (row.ProjectTypeId == Globals.ProjectType.Project)
            {
                cards.Projects.Add(ToPreview(row, divisions, language));
                continue;
            }

            throw new IncidentException(IncidentCode.Fatal, $"{row.ProjectTypeId} is not mapped.");
        }

        return cards;
    }

    private static SharedCardPreview ToPreview(Row row, Dictionary<string, string> divisions, string language)
    {
        var translation = row.Translations.Find(x => x.LanguageId == language)
            ?? row.Translations.FirstOrDefault();

        return new SharedCardPreview
        {
            Id = row.Id,
            ProjectTypeId = row.ProjectTypeId,
            DivisionId = row.DivisionId,
            DivisionName = divisions.GetValueOrDefault(row.DivisionId, row.DivisionId),
            Title = translation?.Title ?? string.Empty,
            Subtitle = translation?.Subtitle ?? string.Empty,
            PosterAlt = translation?.PosterAlt ?? string.Empty,
            PosterUrl = row.PosterUrl,
            LinksJson = row.LinksJson,
            PublishedAt = row.PublishedAt,
        };
    }
}
