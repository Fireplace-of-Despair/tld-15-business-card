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

namespace tld15Server.Features.Rss;

/// <summary>
/// The works the feed announces, newest first. Projects and articles are the same table split by a
/// type and are read on the same page, so the feed carries both: a reader subscribed to this body of
/// work wants to hear about all of it.
/// </summary>
public sealed class RssGetFeature : IFeature
{
    public const string Id = "rss.get";
    public static string FeatureId => Id;
    private const int MaxItems = 20;

    public sealed class Result
    {
        public List<Entry> Entries { get; set; } = [];
    }

    public sealed record Entry(
        string Id,
        string Title,
        string Subtitle,
        DateTimeOffset PublishedAt);

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory)
        : IQueryHandler<Query, Result>
    {
        private sealed record TranslationRow(string LanguageId, string Title, string Subtitle);

        private sealed record Row(
            string Id,
            DateTimeOffset PublishedAt,
            List<TranslationRow> Translations);

        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            await using (var context = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                // Every locale the work carries rather than two of them, because the choice below is
                // the same one the reading page makes and it ends with whichever locale exists. The
                // body stays out: a feed announces a work, it does not carry one.
                var rows = await context
                    .Projects
                    .OrderByDescending(x => x.PublishedAt)
                    .Take(MaxItems)
                    .Select(x => new Row(
                        x.Id,
                        x.PublishedAt,
                        x.Translations
                            .Select(tr => new TranslationRow(tr.LanguageId, tr.Title, tr.Subtitle))
                            .ToList()))
                    .AsNoTracking()
                    .ToListAsync(ctn);

                var result = new Result();

                foreach (var row in rows)
                {
                    // The locale asked for, the locale the site falls back to, or whichever one the
                    // work carries - the same order ProjectReadPage picks in, so the feed never
                    // announces a work the page will not open and never skips one it will.
                    var translation = row.Translations.Find(x => x.LanguageId == query.Language)
                        ?? row.Translations.Find(x => x.LanguageId == Globals.LanguageFallback)
                        ?? row.Translations.FirstOrDefault();

                    // A work nobody has written a word of has no page to point at.
                    if (translation == null) { continue; }

                    result.Entries.Add(new Entry(
                        row.Id,
                        translation.Title,
                        translation.Subtitle,
                        row.PublishedAt));
                }

                return result;
            }
        }
    }
}
