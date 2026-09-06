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
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;
using StainlessInfrastructure.Models.Business;
using tld15Server.Features.Shared.Business;
using tld15Server.Services;

namespace tld15Server.Features.Presses;

/// <summary>
/// Stores a mention whole, creating it when the command carries no id. The mention and all of its
/// translations are written on the same save, so their dates and versions stay in step.
/// </summary>
/// <remarks>
/// The command is the new state of the mention, not a patch of it: a locale the command leaves out
/// is a locale the mention loses. The address it points at has to be one a browser may follow, and
/// the picture one a browser may load — neither is served from here.
/// </remarks>
public sealed class PressPostFeature : IFeature
{
    public const string Id = "press.post";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required SharedPress Press { get; set; }

        /// <summary> The version the caller read. Ignored while the mention is being created. </summary>
        public required long VersionLocal { get; set; }
    }

    public sealed record Result
    {
        public required Guid Id { get; set; }
    }

    public sealed class Handler(
          IDbContextFactory<DataContextBusiness> contextBusinessFactory
        , IDbContextFactory<DataContextReference> contextReferenceFactory
        ) : ICommandHandler<Command, Result>
    {
        public async ValueTask<Result> Handle(Command cmd, CancellationToken ctn)
        {
            var url = UrlPolicy.Clean(cmd.Press.Url);
            var posterUrl = UrlPolicy.Clean(cmd.Press.PosterUrl);

            // A mention leads off this site by definition, so a bare path is not one: the address has
            // to name where it goes. A card that leads nowhere is the one thing this card cannot be.
            if (UrlPolicy.SchemeOf(url).Length == 0
                || !UrlPolicy.IsFollowable(url)
                || !UrlPolicy.IsImageSource(posterUrl))
            {
                throw new IncidentException(IncidentCode.Validation);
            }

            var languages = await LoadLanguages(ctn);
            var translations = ToTranslations(cmd.Press.Translations, languages);

            await using (var contextBusiness = await contextBusinessFactory.CreateDbContextAsync(ctn))
            {
                Press? press = null;

                if (cmd.Press.Id == null)
                {
                    press = new Press { Id = Guid.NewGuid() };
                    await contextBusiness.AddAsync(press, ctn);
                }
                else
                {
                    press = await contextBusiness
                        .Presses
                        .Include(x => x.Translations)
                        .FirstOrDefaultAsync(x => x.Id == cmd.Press.Id, ctn)
                        ?? throw new IncidentException(IncidentCode.NotFound);

                    if (press.VersionLocal != cmd.VersionLocal)
                    {
                        throw new IncidentException(IncidentCode.VersionMismatch);
                    }
                }

                press.Url = url;
                press.PosterUrl = posterUrl;
                press.PublishedAt = cmd.Press.PublishedAt;

                Write(contextBusiness, press, translations);

                // The triggers own the dates and the versions, but they only fire on a row the save
                // actually writes. Touching the version sends the update for a mention whose own
                // columns did not change, so the whole of it moves as one.
                press.VersionLocal++;

                await contextBusiness.SaveChangesAsync(ctn);

                return new Result { Id = press.Id };
            }
        }

        private async Task<HashSet<string>> LoadLanguages(CancellationToken ctn)
        {
            await using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                return await contextReference.Languages.Select(x => x.Id).ToHashSetAsync(ctn);
            }
        }

        /// <summary>
        /// One translation per locale, keeping only the locales that carry something. A locale an
        /// editor left blank is not a translation, and storing it would put an empty title on a card.
        /// </summary>
        private static Dictionary<string, SharedPressTranslation> ToTranslations(
            List<SharedPressTranslation> translations,
            HashSet<string> languages)
        {
            var result = new Dictionary<string, SharedPressTranslation>(StringComparer.Ordinal);

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
            Press press,
            Dictionary<string, SharedPressTranslation> translations)
        {
            foreach (var stored in press.Translations.ToList())
            {
                if (translations.ContainsKey(stored.LanguageId)) { continue; }

                // A locale the editor emptied is a locale the mention no longer speaks.
                context.Remove(stored);
                press.Translations.Remove(stored);
            }

            foreach (var (languageId, translation) in translations)
            {
                var stored = press.Translations.FirstOrDefault(x => x.LanguageId == languageId);

                if (stored == null)
                {
                    stored = new PressTranslation
                    {
                        Id = Guid.NewGuid(),
                        PressId = press.Id,
                        LanguageId = languageId,
                    };

                    press.Translations.Add(stored);
                }

                stored.Title = translation.Title.Trim();
                stored.Subtitle = translation.Subtitle.Trim();
                stored.PosterAlt = translation.PosterAlt.Trim();

                // Same reason as the row above: a translation nothing changed still has to move.
                stored.VersionLocal++;
            }
        }
    }
}
