// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using StainlessCore.Common.Helpers;
using StainlessCore.Exceptions;
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
        List<TranslationRow> Translations,
        List<KeyValuePair<string, string>> DivisionNames);

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
                    .ToList(),
                x.Division.Translations
                    .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                    .Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                    .ToList()));
    }

    /// <summary>
    /// Splits the rows by what they are. The database already ordered them, so a single pass keeps
    /// both lists newest first.
    /// </summary>
    internal static Cards Split(List<Row> rows, string language)
    {
        var cards = new Cards();

        foreach (var row in rows)
        {
            if (row.ProjectTypeId == Globals.ProjectType.Article)
            {
                cards.Articles.Add(ToPreview(row, language));
                continue;
            }

            if (row.ProjectTypeId == Globals.ProjectType.Project)
            {
                cards.Projects.Add(ToPreview(row, language));
                continue;
            }

            throw new IncidentException(IncidentCode.Fatal, $"{row.ProjectTypeId} is not mapped.");
        }

        return cards;
    }

    private static SharedCardPreview ToPreview(Row row, string language)
    {
        var translation = row.Translations.Find(x => x.LanguageId == language)
            ?? row.Translations.FirstOrDefault();

        return new SharedCardPreview
        {
            Id = row.Id,
            ProjectTypeId = row.ProjectTypeId,
            DivisionId = row.DivisionId,
            DivisionName = row.DivisionNames.GetName(language),
            Title = translation?.Title ?? string.Empty,
            Subtitle = translation?.Subtitle ?? string.Empty,
            PosterAlt = translation?.PosterAlt ?? string.Empty,
            PosterUrl = row.PosterUrl,
            LinksJson = row.LinksJson,
            PublishedAt = row.PublishedAt,
        };
    }
}
