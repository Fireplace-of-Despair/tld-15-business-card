// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
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

        public List<SharedCardPreview> Projects { get; set; } = [];
        public List<SharedCardPreview> Articles { get; set; } = [];
    }

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> dataContextBusiness) : IQueryHandler<Query, Result>
    {
        private sealed record ContentRow(
            string ContentId,
            string LanguageId,
            string Name,
            string PosterUrl,
            string PosterAlt,
            string? Markdown,
            string? Json);

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
                        .Select(tr => new ContentRow(x.Id, tr.LanguageId, tr.Name, x.PosterUrl, tr.PosterAlt, tr.Markdown, tr.Json)))
                    .ToListAsync(ctn);

                result.Lore = MapContent(Globals.Content.Lore, contentRows, language);
                result.Social = MapContent(Globals.Content.Social, contentRows, language);
                result.Contacts = MapContent(Globals.Content.Contacts, contentRows, language);

                // The works of the archive division are kept off the front page: they have a page of
                // their own, and the wall here is what the site is doing now rather than what it did.
                var rows = await contextBusiness
                    .Projects
                    .Where(x => x.DivisionId != Globals.Archive.DivisionId)
                    .SelectCards(language, fallback)
                    .ToListAsync(ctn);

                var cards = SharedProjectQuery.Split(rows, language);

                result.Articles = cards.Articles;
                result.Projects = cards.Projects;
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
                PosterUrl = row?.PosterUrl ?? string.Empty,
                PosterAlt = row?.PosterAlt ?? string.Empty,
                Markdown = row?.Markdown,
                Json = row?.Json,
            };
        }
    }
}
