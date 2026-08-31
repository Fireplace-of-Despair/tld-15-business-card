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
using StainlessCore.Service;
using StainlessInfrastructure;
using StainlessInfrastructure.Models.Identity;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Features.Accounts;

public sealed class AccountsPostFeature : IFeature
{
    public const string Id = "accounts.post";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required SharedAccount Account { get; set; }
    }

    public sealed record Result
    {
        public required Guid Id { get; set; }

        public List<SharedApiKey> CreatedApiKeys { get; set; } = [];
    }

    public sealed class Handler(
          IDbContextFactory<DataContextIdentity> contextIdentityFactory
        , HashingService hashingService

        , ApiKeyService apiKeyService
        ) : ICommandHandler<Command, Result>
    {
        public async ValueTask<Result> Handle(Command cmd, CancellationToken ctn)
        {
            Account? account = null;

            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                if (cmd.Account.Id == null)
                {
                    account = new Account { Id = Guid.NewGuid() };
                    await contextIdentity.AddAsync(account, ctn);
                }
                else
                {
                    account = await contextIdentity.Accounts
                        .Include(x => x.Features)
                        .Include(x => x.ApiKeys)
                        .FirstOrDefaultAsync(x => x.Id == cmd.Account.Id, ctn)
                        ?? throw new IncidentException(IncidentCode.NotFound);
                }

                Validate(account, cmd);

                account.Login = cmd.Account.Login;
                account.AccountStatusId = cmd.Account.StatusId;
                account.AccountTypeId = cmd.Account.TypeId;

                foreach (var feature in cmd.Account.Features)
                {
                    if (account.Features.Any(x => x.Id == feature))
                    {
                        continue;
                    }

                    var tmp = new Feature { Id = feature };
                    contextIdentity.Attach(tmp);
                    account.Features.Add(tmp);
                }
                var currentFeatures = account.Features.Select(x => x.Id).ToList();
                foreach (var item in currentFeatures)
                {
                    if (cmd.Account.Features.Contains(item))
                    {
                        continue;
                    }
                    account.Features.Remove(account.Features.First(x => x.Id == item));
                }

                var createdApiKeys = SyncApiKeys(account, cmd.Account.ApiKeys, apiKeyService);

                if (!string.IsNullOrEmpty(cmd.Account.Password))
                {
                    var hashResult = hashingService.Hash(cmd.Account.Password);
                    account.Password = hashResult.HexHash;
                    account.Salt = hashResult.HexSalt;
                }

                //NOTE: EF will not update the entity, if only the relationships changed
                account.VersionLocal++;

                await contextIdentity.SaveChangesAsync(ctn);

                return new Result { Id = account.Id, CreatedApiKeys = createdApiKeys };
            }
        }

        /// <summary>
        /// Stores a key for every entry the caller sent without an id, and drops the ones no longer listed.
        /// Existing keys are left untouched: only their hash is stored, so there is nothing to edit.
        /// </summary>
        /// <remarks>
        /// The UI generates the key when the user adds it, so it can be shown once before saving; the
        /// value arrives here and is hashed. A caller that sends no value gets one generated instead,
        /// so a key can never end up stored as something the caller chose.
        /// </remarks>
        internal static List<SharedApiKey> SyncApiKeys(Account account, List<SharedApiKey> requested, ApiKeyService apiKeyService)
        {
            var created = new List<SharedApiKey>();

            foreach (var requestedKey in requested.Where(x => x.Id == null))
            {
                var value = string.IsNullOrWhiteSpace(requestedKey.Value)
                    ? ApiKeyService.Generate()
                    : requestedKey.Value;

                var apiKey = new ApiKey
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    KeyHash = apiKeyService.Hash(value)
                };

                account.ApiKeys.Add(apiKey);
                created.Add(new SharedApiKey { Id = apiKey.Id, Value = value });
            }

            var keptIds = requested.Where(x => x.Id != null).Select(x => x.Id!.Value).ToHashSet();
            foreach (var removed in account.ApiKeys.Where(x => !keptIds.Contains(x.Id) && !created.Exists(c => c.Id == x.Id)).ToList())
            {
                account.ApiKeys.Remove(removed);
            }

            return created;
        }

        private static void Validate(Account account, Command cmd)
        {
            if (account.VersionLocal != cmd.Account.VersionLocal)
            {
                throw new IncidentException(IncidentCode.VersionMismatch);
            }
        }
    }
}
