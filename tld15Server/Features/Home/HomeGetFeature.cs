// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Common.Helpers;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Home;

public sealed class HomeGetFeature : IFeature
{
    public const string Id = "home.get";
    public static string FeatureId => Id;

    public sealed class Result
    {
        public SharedContent Lore { get; set; } = new();
        public SharedContent Social { get; set; } = new();
        public SharedContent Contacts { get; set; } = new();

        public List<SharedProjectPreview> Projects { get; set; } = [];
        public List<SharedProjectPreview> Articles { get; set; } = [];
    }

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> dataContextBusiness) : IQueryHandler<Query, Result>
    {
        private sealed record ContentRow(string ContentId, string LanguageId, string Name, string? Markdown, string? Json);

        private sealed record ProjectTranslationRow(string LanguageId, string Title, string Subtitle, string PosterAlt);

        private sealed record ProjectRow(
            string Id,
            string ProjectTypeId,
            string DivisionId,
            string PosterUrl,
            string? LinksJson,
            DateTimeOffset CreatedAt,
            List<ProjectTranslationRow> Translations,
            List<KeyValuePair<string, string>> DivisionNames);

        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var language = query.Language;
            var fallback = Globals.LanguageFallback;

            var result = new Result();

            await using (var contextBusiness = await dataContextBusiness.CreateDbContextAsync(ctn))
            {
                var contentIds = new[] { Globals.Content.Lore, Globals.Content.Social, Globals.Content.Contacts };

                // Two locales leave the database instead of the whole translation set: the requested one and
                // the fallback the page renders when the requested one holds no row yet.
                var contentRows = await contextBusiness
                    .Contents
                    .Where(x => contentIds.Contains(x.Id))
                    .SelectMany(x => x.Translations
                        .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                        .Select(tr => new ContentRow(x.Id, tr.LanguageId, tr.Name, tr.Markdown, tr.Json)))
                    .ToListAsync(ctn);

                result.Lore = MapContent(Globals.Content.Lore, contentRows, language);
                result.Social = MapContent(Globals.Content.Social, contentRows, language);
                result.Contacts = MapContent(Globals.Content.Contacts, contentRows, language);

                var projectTypeIds = new[] { Globals.ProjectType.Project, Globals.ProjectType.Article };

                // content_html stays out of the projection on purpose. The card carries a title, a subtitle and
                // a poster, so pulling the body of every article would dominate the payload of the page.
                var projectRows = await contextBusiness
                    .Projects
                    .Where(x => projectTypeIds.Contains(x.ProjectTypeId))
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new ProjectRow(
                        x.Id,
                        x.ProjectTypeId,
                        x.DivisionId,
                        x.PosterUrl,
                        x.LinksJson,
                        x.CreatedAt,
                        x.Translations
                            .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                            .Select(tr => new ProjectTranslationRow(tr.LanguageId, tr.Title, tr.Subtitle, tr.PosterAlt))
                            .ToList(),
                        x.Division.Translations
                            .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                            .Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                            .ToList()))
                    .ToListAsync(ctn);

                // The database already ordered the rows, so a single pass keeps both lists newest first.
                foreach (var row in projectRows)
                {
                    if (row.ProjectTypeId == Globals.ProjectType.Article)
                    {
                        result.Articles.Add(MapProject(row, language));
                        continue;
                    }

                    if (row.ProjectTypeId == Globals.ProjectType.Project)
                    {
                        result.Projects.Add(MapProject(row, language));
                        continue;
                    }

                    throw new IncidentException(IncidentCode.Fatal, $"{row.ProjectTypeId} is not mapped.");
                }
            }

            return result;
        }

        /// <summary>
        /// Picks the requested locale, falls back to the other locale the query loaded, and leaves the block
        /// empty when the content holds no translation at all. The page skips an empty block.
        /// </summary>
        private static SharedContent MapContent(string contentId, List<ContentRow> rows, string language)
        {
            var row = rows.Find(x => x.ContentId == contentId && x.LanguageId == language)
                ?? rows.Find(x => x.ContentId == contentId);

            return new SharedContent
            {
                Id = contentId,
                Title = row?.Name ?? string.Empty,
                Markdown = row?.Markdown,
                Json = row?.Json,
            };
        }

        private static SharedProjectPreview MapProject(ProjectRow row, string language)
        {
            var translation = row.Translations.Find(x => x.LanguageId == language)
                ?? row.Translations.FirstOrDefault();

            return new SharedProjectPreview
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
                CreatedAt = row.CreatedAt,
            };
        }
    }
}
