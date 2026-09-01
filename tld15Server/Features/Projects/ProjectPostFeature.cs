// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;
using StainlessInfrastructure.Models.Business;
using tld15Server.Composition;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Features.Projects;

/// <summary>
/// Stores a project whole, creating it when the id is one the table does not carry yet. The project
/// and its translations are written on the same save, so their dates and versions stay in step and
/// the version of the project alone says whether a caller holds a stale copy.
/// </summary>
/// <remarks>
/// The command is the new state of the project, not a patch of it: a locale the command leaves out
/// is a locale the project loses. Posters are addresses, never uploads, so the only thing stored
/// about one is a url a browser may load a picture from.
/// </remarks>
public sealed partial class ProjectPostFeature : IFeature
{
    public const string Id = "project.post";
    public static string FeatureId => Id;

    [GeneratedRegex(Globals.Project.IdPattern)]
    private static partial Regex IdShape();

    public sealed record Command : ICommand<Result>
    {
        public required SharedProject Project { get; set; }

        /// <summary> The version the caller read. Ignored while the project is being created. </summary>
        public required long VersionLocal { get; set; }
    }

    public sealed record Result
    {
        public required string Id { get; set; }
    }

    public sealed class Handler(
          IDbContextFactory<DataContextBusiness> contextBusinessFactory
        , IDbContextFactory<DataContextReference> contextReferenceFactory
        ) : ICommandHandler<Command, Result>
    {
        public async ValueTask<Result> Handle(Command cmd, CancellationToken ctn)
        {
            var id = cmd.Project.Id.Trim().ToLowerInvariant();

            if (id.Length == 0 || id.Length > Globals.Project.IdMaxLength || !IdShape().IsMatch(id))
            {
                throw new IncidentException(IncidentCode.Validation);
            }

            var posterUrl = UrlPolicy.Clean(cmd.Project.PosterUrl);

            if (!UrlPolicy.IsImageSource(posterUrl))
            {
                throw new IncidentException(IncidentCode.Validation);
            }

            var references = await LoadReferences(ctn);

            if (!references.Divisions.Contains(cmd.Project.DivisionId)
                || !references.Types.Contains(cmd.Project.ProjectTypeId))
            {
                throw new IncidentException(IncidentCode.Validation);
            }

            var links = ToLinks(cmd.Project.Links);
            var translations = ToTranslations(cmd.Project.Translations, references.Languages);

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var project = await contextBusiness
                    .Projects
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync(x => x.Id == id, ctn);

                if (project == null)
                {
                    project = new Project { Id = id };
                    await contextBusiness.AddAsync(project, ctn);
                }
                else if (project.VersionLocal != cmd.VersionLocal)
                {
                    throw new IncidentException(IncidentCode.VersionMismatch);
                }

                project.ProjectTypeId = cmd.Project.ProjectTypeId;
                project.DivisionId = cmd.Project.DivisionId;
                project.PosterUrl = posterUrl;
                project.PublishedAt = cmd.Project.PublishedAt;
                project.LinksJson = LinkJson.ToJson(links);

                Write(contextBusiness, project, translations);

                // The triggers own the dates and the versions, but they only fire on a row the save
                // actually writes. Touching the version sends the update for a project whose own
                // columns did not change, so the whole of it moves as one.
                project.VersionLocal++;

                await contextBusiness.SaveChangesAsync(ctn);

                return new Result { Id = project.Id };
            }
        }

        private sealed record References(HashSet<string> Languages, HashSet<string> Divisions, HashSet<string> Types);

        private async Task<References> LoadReferences(CancellationToken ctn)
        {
            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                return new References
                (
                    await contextReference.Languages.Select(x => x.Id).ToHashSetAsync(ctn),
                    await contextReference.Divisions.Select(x => x.Id).ToHashSetAsync(ctn),
                    await contextReference.ProjectTypes.Select(x => x.Id).ToHashSetAsync(ctn)
                );
            }
        }

        /// <summary>
        /// The links of the project as one dictionary. A row without an icon or without an address is
        /// not a link and drops out. Two rows landing on the same key are a mistake the caller has to
        /// resolve, not one this handler resolves by keeping whichever row came last.
        /// </summary>
        private static Dictionary<string, string> ToLinks(List<SharedLink> links)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var link in links)
            {
                var icon = link.Icon.Trim().ToLowerInvariant();
                var language = link.Language.Trim().ToLowerInvariant();
                var url = UrlPolicy.Clean(SharedLink.ToStoredUrl(link.Url));

                if (string.IsNullOrEmpty(icon) || string.IsNullOrEmpty(url)) { continue; }

                if (!SharedLink.IsLanguageValid(language) || !UrlPolicy.IsFollowable(url))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }

                if (!result.TryAdd(SharedLink.ToKey(icon, language), url))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }
            }

            return result;
        }

        /// <summary>
        /// One translation per locale, keeping only the locales that carry something. A locale an
        /// editor left blank is not a translation, and storing it would put an empty title on a card.
        /// </summary>
        private static Dictionary<string, SharedProjectTranslation> ToTranslations(
            List<SharedProjectTranslation> translations,
            HashSet<string> languages)
        {
            var result = new Dictionary<string, SharedProjectTranslation>(StringComparer.Ordinal);

            foreach (var translation in translations)
            {
                if (translation.IsEmpty) { continue; }

                if (!languages.Contains(translation.LanguageId) || !result.TryAdd(translation.LanguageId, translation))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }
            }

            return result;
        }

        /// <summary> Brings the stored translations to exactly the set the command carries. </summary>
        private static void Write(
            DataContextBusiness context,
            Project project,
            Dictionary<string, SharedProjectTranslation> translations)
        {
            foreach (var stored in project.Translations.ToList())
            {
                if (translations.ContainsKey(stored.LanguageId)) { continue; }

                // A locale the editor emptied is a locale the project no longer speaks.
                context.Remove(stored);
                project.Translations.Remove(stored);
            }

            foreach (var (languageId, translation) in translations)
            {
                var stored = project.Translations.FirstOrDefault(x => x.LanguageId == languageId);

                if (stored == null)
                {
                    stored = new ProjectTranslation
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        LanguageId = languageId,
                    };

                    project.Translations.Add(stored);
                }

                stored.Title = translation.Title.Trim();
                stored.Subtitle = translation.Subtitle.Trim();
                stored.PosterAlt = translation.PosterAlt.Trim();
                stored.Markdown = string.IsNullOrWhiteSpace(translation.Markdown) ? null : translation.Markdown;

                // Same reason as the project row: a translation nothing changed still has to move.
                stored.VersionLocal++;
            }
        }
    }
}
