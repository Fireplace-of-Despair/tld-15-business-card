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

namespace tld15Server.Features.Contents;

/// <summary>
/// Stores the links of a content, replacing every locale at once. The content and all of its
/// translations are written on the same save, so their dates and versions stay in step and the
/// version of the content alone says whether a caller is holding a stale copy.
/// </summary>
public sealed class ContentPostFeature : IFeature
{
    public const string Id = "content.post";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required string Id { get; set; }
        public List<SharedContentLink> Links { get; set; } = [];

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
            var languages = await LoadLanguages(ctn);
            var links = ToLinks(cmd.Links, languages);

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

                foreach (var languageId in links.Keys.Where(x => !content.Translations.Any(tr => tr.LanguageId == x)))
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
                    translation.Json = links.TryGetValue(translation.LanguageId, out var stored)
                        ? ContentJson.ToJson(stored)
                        : null;

                    // The triggers own the dates and the versions, but they only fire on a row the
                    // save actually writes. Touching the version sends the update for a translation
                    // whose links did not change, so the whole content moves as one.
                    translation.VersionLocal++;
                }

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

                if (!languages.Contains(link.TranslationLanguageId) || !SharedContentLink.IsLanguageValid(language))
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
