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
using StainlessCore.Models;
using StainlessInfrastructure;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Contents;

/// <summary>
/// Reads one content whole: the body and the links of every locale it carries. The dates and the
/// version belong to the content itself, because a save moves the content and all of its
/// translations together, so the root row is the one number that describes the whole of it.
/// </summary>
/// <remarks>
/// An editor page opens one side of a content and leaves the other alone, so both sides leave the
/// database on every read: <see cref="ContentPostFeature"/> stores what it is given and nothing
/// else, and a page that dropped the side it does not edit would erase it on the next save.
/// </remarks>
public sealed class ContentGetFeature : IFeature
{
    public const string Id = "content.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required string Id { get; set; }

        /// <summary> The locale the caller reads in. It picks the title, not the links. </summary>
        public required string LanguageId { get; set; }
    }

    public sealed record Result : IUpdatable, IVersionLocal
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public List<SharedContentLink> Links { get; set; } = [];
        public Dictionary<string, string> Markdown { get; set; } = [];
        public Dictionary<string, string> Languages { get; set; } = [];

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public long VersionLocal { get; set; }
    }

    public sealed class Handler(
          IDbContextFactory<DataContextBusiness> contextBusinessFactory
        , IDbContextFactory<DataContextReference> contextReferenceFactory
        ) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var result = await LoadContent(query, ctn);

            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                result.Languages = await contextReference
                    .Languages
                    .OrderBy(x => x.Id)
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ctn);
            }

            return result;
        }

        private async Task<Result> LoadContent(Query query, CancellationToken ctn)
        {
            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var content = await contextBusiness
                    .Contents
                    .Where(x => x.Id == query.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.CreatedAt,
                        x.UpdatedAt,
                        x.VersionLocal,
                        Titles = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name)).ToList(),
                        Translations = x.Translations.Select(tr => new { tr.LanguageId, tr.Json, tr.Markdown }).ToList()
                    })
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                return new Result
                {
                    Id = content.Id,
                    Title = content.Titles.GetName(query.LanguageId),
                    CreatedAt = content.CreatedAt,
                    UpdatedAt = content.UpdatedAt,
                    VersionLocal = content.VersionLocal,
                    Links = [.. content.Translations
                        .OrderBy(x => x.LanguageId, StringComparer.Ordinal)
                        .SelectMany(tr => LinkJson
                            .ToDictionary(tr.Json)
                            .Select(link => SharedContentLink.FromStoredOf(tr.LanguageId, link.Key, link.Value)))],
                    Markdown = content.Translations
                        .Where(x => !string.IsNullOrEmpty(x.Markdown))
                        .ToDictionary(x => x.LanguageId, x => x.Markdown!, StringComparer.Ordinal)
                };
            }
        }
    }
}
