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
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Presses;

/// <summary>
/// Every mention, newest first, as the cards the public page draws. The table is short and the page
/// shows all of it: a wall of press is read by scrolling, not by turning pages.
/// </summary>
public sealed class PressListFeature : IFeature
{
    public const string Id = "press.list";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string Language { get; set; }
    }

    public sealed class Result
    {
        public List<SharedCardPreview> Items { get; set; } = [];
    }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> contextBusinessFactory) : IQueryHandler<Query, Result>
    {
        private sealed record TranslationRow(string LanguageId, string Title, string Subtitle, string PosterAlt);

        private sealed record Row(
            Guid Id,
            string Url,
            string PosterUrl,
            DateTimeOffset PublishedAt,
            List<TranslationRow> Translations);

        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var language = query.Language;
            var fallback = Globals.LanguageFallback;

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                // Two locales leave the database: the requested one and the fallback a card falls
                // back to when the requested one holds no row.
                var rows = await contextBusiness
                    .Presses
                    .OrderByDescending(x => x.PublishedAt)
                    .Select(x => new Row(
                        x.Id,
                        x.Url,
                        x.PosterUrl,
                        x.PublishedAt,
                        x.Translations
                            .Where(tr => tr.LanguageId == language || tr.LanguageId == fallback)
                            .Select(tr => new TranslationRow(tr.LanguageId, tr.Title, tr.Subtitle, tr.PosterAlt))
                            .ToList()))
                    .AsNoTracking()
                    .ToListAsync(ctn);

                return new Result
                {
                    Items = rows.ConvertAll(row =>
                    {
                        var translation = row.Translations.Find(x => x.LanguageId == language)
                            ?? row.Translations.FirstOrDefault();

                        return new SharedCardPreview
                        {
                            Id = row.Id.ToString(),
                            Title = translation?.Title ?? string.Empty,
                            Subtitle = translation?.Subtitle ?? string.Empty,
                            PosterAlt = translation?.PosterAlt ?? string.Empty,
                            PosterUrl = row.PosterUrl,
                            PublishedAt = row.PublishedAt,
                            ExternalUrl = row.Url,
                        };
                    })
                };
            }
        }
    }
}
