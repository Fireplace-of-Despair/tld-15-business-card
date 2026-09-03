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

namespace tld15Server.Features.Projects;

/// <summary>
/// Reads one project whole, together with the reference sets the editor picks from. A query with no
/// id opens a project that does not exist yet: the same shape, empty, so the editor has one path.
/// </summary>
public sealed class ProjectGetFeature : IFeature
{
    public const string Id = "project.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        /// <summary> The project to read, or null to start a new one. </summary>
        public string? Id { get; set; }

        /// <summary> The locale the caller reads the reference names in. </summary>
        public required string LanguageId { get; set; }
    }

    public sealed record Result : IUpdatable, IVersionLocal
    {
        public SharedProject Item { get; set; } = new();

        /// <summary> Every locale the application stores, as an id to a name. </summary>
        public Dictionary<string, string> Languages { get; set; } = [];

        public Dictionary<string, string> Divisions { get; set; } = [];

        public Dictionary<string, string> ProjectTypes { get; set; } = [];

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
            var result = string.IsNullOrEmpty(query.Id)
                ? new Result { Item = new SharedProject { PublishedAt = DateTimeOffset.UtcNow } }
                : await LoadProject(query.Id, ctn);

            await LoadReferences(result, query.LanguageId, ctn);

            // A project that does not exist yet still has to open on something the editor can store.
            if (string.IsNullOrEmpty(result.Item.Id))
            {
                result.Item.ProjectTypeId = result.ProjectTypes.Keys.FirstOrDefault() ?? string.Empty;
                result.Item.DivisionId = result.Divisions.Keys.FirstOrDefault() ?? string.Empty;
            }

            return result;
        }

        private async Task<Result> LoadProject(string id, CancellationToken ctn)
        {
            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var project = await contextBusiness
                    .Projects
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ProjectTypeId,
                        x.DivisionId,
                        x.PosterUrl,
                        x.LinksJson,
                        x.PublishedAt,
                        x.CreatedAt,
                        x.UpdatedAt,
                        x.VersionLocal,
                        Translations = x.Translations
                            .Select(tr => new { tr.LanguageId, tr.Title, tr.Subtitle, tr.PosterAlt, tr.Markdown })
                            .ToList()
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                return new Result
                {
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt,
                    VersionLocal = project.VersionLocal,
                    Item = new SharedProject
                    {
                        Id = project.Id,
                        ProjectTypeId = project.ProjectTypeId,
                        DivisionId = project.DivisionId,
                        PosterUrl = project.PosterUrl,
                        PublishedAt = project.PublishedAt,
                        Links = [.. SharedLink
                            .JsonToDictionary(project.LinksJson)
                            .Select(link => SharedLink.FromStored(link.Key, link.Value))],
                        Translations = [.. project.Translations
                            .OrderBy(x => x.LanguageId, StringComparer.Ordinal)
                            .Select(tr => new SharedProjectTranslation
                            {
                                LanguageId = tr.LanguageId,
                                Title = tr.Title,
                                Subtitle = tr.Subtitle,
                                PosterAlt = tr.PosterAlt,
                                Markdown = tr.Markdown ?? string.Empty,
                            })]
                    }
                };
            }
        }

        private async Task LoadReferences(Result result, string languageId, CancellationToken ctn)
        {
            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                result.Languages = await contextReference
                    .Languages
                    .OrderBy(x => x.Id)
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ctn);

                var divisions = await contextReference
                    .Divisions
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        Names = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                    })
                    .ToListAsync(ctn);

                var types = await contextReference
                    .ProjectTypes
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        Names = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                    })
                    .ToListAsync(ctn);

                result.Divisions = divisions.ToDictionary(x => x.Id, x => x.Names.GetName(languageId), StringComparer.Ordinal);
                result.ProjectTypes = types.ToDictionary(x => x.Id, x => x.Names.GetName(languageId), StringComparer.Ordinal);
            }
        }
    }
}
