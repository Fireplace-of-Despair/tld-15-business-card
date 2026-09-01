// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessCore.Models;
using StainlessInfrastructure;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Features.Presses;

/// <summary>
/// Reads one mention whole, with the locales the editor may write it in. A query with no id opens a
/// mention that does not exist yet: the same shape, empty, so the editor has one path.
/// </summary>
public sealed class PressGetFeature : IFeature
{
    public const string Id = "press.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        /// <summary> The mention to read, or null to start a new one. </summary>
        public Guid? Id { get; set; }
    }

    public sealed record Result : IUpdatable, IVersionLocal
    {
        public SharedPress Item { get; set; } = new();

        /// <summary> Every locale the application stores, as an id to a name. </summary>
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
            var result = query.Id == null
                ? new Result { Item = new SharedPress { PublishedAt = DateTimeOffset.UtcNow } }
                : await LoadPress(query.Id.Value, ctn);

            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                result.Languages = await contextReference
                    .Languages
                    .OrderBy(x => x.Id)
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ctn);
            }

            return result;
        }

        private async Task<Result> LoadPress(Guid id, CancellationToken ctn)
        {
            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var press = await contextBusiness
                    .Presses
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Url,
                        x.PosterUrl,
                        x.PublishedAt,
                        x.CreatedAt,
                        x.UpdatedAt,
                        x.VersionLocal,
                        Translations = x.Translations
                            .Select(tr => new { tr.LanguageId, tr.Title, tr.Subtitle, tr.PosterAlt })
                            .ToList()
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                return new Result
                {
                    CreatedAt = press.CreatedAt,
                    UpdatedAt = press.UpdatedAt,
                    VersionLocal = press.VersionLocal,
                    Item = new SharedPress
                    {
                        Id = press.Id,
                        Url = press.Url,
                        PosterUrl = press.PosterUrl,
                        PublishedAt = press.PublishedAt,
                        Translations = [.. press.Translations
                            .OrderBy(x => x.LanguageId, StringComparer.Ordinal)
                            .Select(tr => new SharedPressTranslation
                            {
                                LanguageId = tr.LanguageId,
                                Title = tr.Title,
                                Subtitle = tr.Subtitle,
                                PosterAlt = tr.PosterAlt,
                            })]
                    }
                };
            }
        }
    }
}
