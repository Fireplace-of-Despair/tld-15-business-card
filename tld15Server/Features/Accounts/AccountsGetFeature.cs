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
using StainlessCore.Common.Helpers;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessInfrastructure;
using tld15Server.Composition;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Features.Accounts;

public sealed class AccountsGetFeature : IFeature
{
    public const string Id = "accounts.get";
    public static string FeatureId => Id;

    public sealed record Query : IQuery<Result>
    {
        public required Guid? Id { get; set; }
        public required string Language { get; set; }
    }

    public sealed record Result
    {
        public required SharedAccount Item { get; set; }

        public required Dictionary<string, string> Features { get; set; } = [];

        public required Dictionary<string, string> Statuses { get; set; } = [];

        public required Dictionary<string, string> Types { get; set; } = [];
    }

    public sealed class Handler(
          IDbContextFactory<DataContextIdentity> contextIdentityFactory
        , IDbContextFactory<DataContextReference> contextReferenceFactory
    ) : IQueryHandler<Query, Result>
    {
        public async ValueTask<Result> Handle(Query query, CancellationToken ctn)
        {
            var features = await LoadFeatures(query, ctn);
            var statuses = await LoadStatuses(query, ctn);
            var types = await LoadTypes(query, ctn);

            var account = await LoadAccount(query, ctn);

            return new Result
            {
                Item = account,
                Features = features,
                Statuses = statuses,
                Types = types
            };
        }

        private async Task<Dictionary<string, string>> LoadFeatures(Query query, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var feature = await contextIdentity.Features
                    .Select(x => new
                    {
                        x.Id,
                        Translations = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                    })
                    .OrderBy(x => x.Id)
                    .ToListAsync(ctn);

                return feature
                    .Select(x => new
                    {
                        x.Id,
                        Name = x.Translations.GetName(query.Language)
                    })
                    .OrderBy(x => x.Id)
                    .ToDictionary(k => k.Id, v => v.Name);
            }
        }

        private async Task<Dictionary<string, string>> LoadStatuses(Query query, CancellationToken ctn)
        {
            using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                var statuses = await contextReference.AccountStatuses
                .Select(x => new
                {
                    x.Id,
                    Translations = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                })
                .ToListAsync(ctn);

                return statuses
                    .Select(x => new
                    {
                        x.Id,
                        Name = x.Translations.GetName(query.Language)
                    })
                    .ToDictionary(k => k.Id, v => v.Name);
            }
        }

        private async Task<Dictionary<string, string>> LoadTypes(Query query, CancellationToken ctn)
        {
            using (var contextReference = await contextReferenceFactory.CreateDbContextAsync(ctn))
            {
                var types = await contextReference.AccountTypes
                .Select(x => new
                {
                    x.Id,
                    Translations = x.Translations.Select(tr => new KeyValuePair<string, string>(tr.LanguageId, tr.Name))
                })
                .ToListAsync(ctn);

                return types
                    .Select(x => new
                    {
                        x.Id,
                        Name = x.Translations.GetName(query.Language)
                    })
                    .ToDictionary(k => k.Id, v => v.Name);
            }
        }

        private async Task<SharedAccount> LoadAccount(Query query, CancellationToken ctn)
        {
            if (query.Id == null)
            {
                return new SharedAccount
                {
                    Id = null,
                    Features = [],
                    TypeId = Globals.Reference.AccountType.User,
                    Login = string.Empty,
                    Password = null,
                    StatusId = Globals.Reference.AccountStatus.Enabled,
                    ApiKeys = [],
                };
            }

            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var result = await contextIdentity.Accounts
                .AsNoTracking()
                .Where(x => x.Id == query.Id.Value)
                .Select(x => new
                {
                    x.Id,
                    Features = x.Features.Select(x => x.Id).ToList(),
                    x.Login,
                    x.AccountTypeId,
                    x.AccountStatusId,
                    // The hash is never sent to the UI: there is nothing a user could do with it.
                    ApiKeys = x.ApiKeys.Select(key => new { key.Id, key.CreatedAt, key.LastUsedAt }),
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.VersionLocal
                })
                .FirstOrDefaultAsync(ctn)
                ?? throw new IncidentException(IncidentCode.NotFound);


                return new SharedAccount
                {
                    Id = result.Id,
                    Features = result.Features,
                    Login = result.Login,
                    TypeId = result.AccountTypeId,
                    Password = null,
                    StatusId = result.AccountStatusId,
                    ApiKeys = [.. result.ApiKeys.Select(key => new SharedApiKey
                    {
                        Id = key.Id,
                        CreatedAt = key.CreatedAt,
                        LastUsedAt = key.LastUsedAt
                    })],
                    CreatedAt = result.CreatedAt,
                    UpdatedAt = result.UpdatedAt,
                    VersionLocal = result.VersionLocal
                };
            }
        }
    }
}
