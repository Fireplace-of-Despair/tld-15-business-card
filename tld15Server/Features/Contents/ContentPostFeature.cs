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
using StainlessInfrastructure.Models.Business;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Features.Contents;

/// <summary>
/// Stores a content whole — the picture, the body and the links of every locale — replacing what was
/// there. The content and all of its translations are written on the same save, so their dates and
/// versions stay in step and the version of the content alone says whether a caller holds a stale copy.
/// </summary>
/// <remarks>
/// The command is the new state of the content, not a patch of it: a locale the command leaves out
/// loses what it held. A page that edits one side of a content therefore sends the other side back
/// exactly as <see cref="ContentGetFeature"/> handed it over.
/// </remarks>
public sealed class ContentPostFeature : IFeature
{
    public const string Id = "content.post";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required string Id { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
        public List<SharedContentLink> Links { get; set; } = [];
        public Dictionary<string, string?> Markdown { get; set; } = [];
        public Dictionary<string, string?> PosterAlt { get; set; } = [];
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
            var posterUrl = UrlPolicy.Clean(cmd.PosterUrl);

            if (!UrlPolicy.IsImageSource(posterUrl))
            {
                throw new IncidentException(IncidentCode.Validation);
            }

            var languages = await LoadLanguages(ctn);
            var links = ToLinks(cmd.Links, languages);
            var markdown = ToMarkdown(cmd.Markdown, languages);
            var posterAlt = ToPosterAlt(cmd.PosterAlt, languages);

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                var content = await contextBusiness
                    .Contents
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync(x => x.Id == cmd.Id, ctn)
                    ?? throw new IncidentException(IncidentCode.NotFound);

                if (content.VersionLocal != cmd.VersionLocal)
                {
                    throw new IncidentException(IncidentCode.VersionMismatch);
                }

                var written = new HashSet<string>(links.Keys, StringComparer.Ordinal);
                written.UnionWith(markdown.Keys);
                written.UnionWith(posterAlt.Keys);

                foreach (var languageId in written.Where(x => !content.Translations.Any(tr => tr.LanguageId == x)))
                {
                    content.Translations.Add(new ContentTranslation
                    {
                        Id = Guid.NewGuid(),
                        ContentId = content.Id,
                        LanguageId = languageId,
                        Name = content.Translations
                            .Select(x => new KeyValuePair<string, string>(x.LanguageId, x.Name))
                            .GetName(languageId),
                    });
                }

                foreach (var translation in content.Translations)
                {
                    translation.Json = links.TryGetValue(translation.LanguageId, out var storedLinks)
                        ? LinkJson.ToJson(storedLinks)
                        : null;

                    translation.Markdown = markdown.TryGetValue(translation.LanguageId, out var storedText)
                        ? storedText
                        : null;

                    translation.PosterAlt = posterAlt.TryGetValue(translation.LanguageId, out var storedAlt)
                        ? storedAlt
                        : string.Empty;

                    // The triggers own the dates and the versions, but they only fire on a row the
                    // save actually writes. Touching the version sends the update for a translation
                    // whose links did not change, so the whole content moves as one.
                    translation.VersionLocal++;
                }

                content.PosterUrl = posterUrl;
                content.VersionLocal++;

                await contextBusiness.SaveChangesAsync(ctn);

                return new Result { Id = content.Id };
            }
        }

        private async Task<HashSet<string>> LoadLanguages(CancellationToken ctn)
        {
            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                return await contextReference
                    .Languages
                    .Select(x => x.Id)
                    .ToHashSetAsync(ctn);
            }
        }

        /// <summary>
        /// The body of every locale that carries one. A blank text is not a body and drops out, so
        /// an editor that clears the field stores null rather than a row of spaces.
        /// </summary>
        private static Dictionary<string, string> ToMarkdown(
            Dictionary<string, string?> markdown,
            HashSet<string> languages)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var (languageId, text) in markdown)
            {
                if (string.IsNullOrWhiteSpace(text)) { continue; }

                if (!languages.Contains(languageId))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }

                result[languageId] = text;
            }

            return result;
        }

        /// <summary>
        /// What the picture shows, for every locale that says it. A blank description is not one and
        /// drops out, so a locale that carries no text for the picture stores an empty column rather
        /// than a row of spaces a screen reader would read out.
        /// </summary>
        private static Dictionary<string, string> ToPosterAlt(
            Dictionary<string, string?> posterAlt,
            HashSet<string> languages)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var (languageId, text) in posterAlt)
            {
                if (string.IsNullOrWhiteSpace(text)) { continue; }

                if (!languages.Contains(languageId))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }

                result[languageId] = text.Trim();
            }

            return result;
        }

        /// <summary>
        /// Turns the rows of the editor into one dictionary per locale. A row without an icon or
        /// without an address is not a link and drops out. Two rows that would land on the same key
        /// of the same locale are a mistake the caller has to resolve, not one this handler resolves
        /// by keeping whichever row came last.
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ToLinks(
            List<SharedContentLink> links,
            HashSet<string> languages)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (var link in links)
            {
                var icon = link.Icon.Trim().ToLowerInvariant();
                var language = link.Language.Trim().ToLowerInvariant();
                var url = SharedContentLink.ToStoredUrl(link.Url);

                if (string.IsNullOrEmpty(icon) || string.IsNullOrEmpty(url)) { continue; }

                if (!languages.Contains(link.TranslationLanguageId)
                    || !SharedContentLink.IsLanguageValid(language)
                    || !UrlPolicy.IsFollowable(UrlPolicy.Clean(url)))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }

                if (!result.TryGetValue(link.TranslationLanguageId, out var stored))
                {
                    stored = new Dictionary<string, string>(StringComparer.Ordinal);
                    result[link.TranslationLanguageId] = stored;
                }

                if (!stored.TryAdd(SharedContentLink.ToKey(icon, language), url))
                {
                    throw new IncidentException(IncidentCode.Validation);
                }
            }

            return result;
        }
    }
}
