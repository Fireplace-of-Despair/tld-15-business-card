// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
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
using tld15Server.Features.Accounts;
using tld15Server.Features.Shared.Identity;

namespace tld15Server.Features.Profiles;

public class ProfilePutFeature : IFeature
{
    public const string Id = "profile.put";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<Result>
    {
        public required Guid AccountId { get; set; }
        public required SharedAccount Account { get; set; }

        /// <summary> The password in use. Required only when <see cref="SharedAccount.Password"/> sets a new one. </summary>
        public string? CurrentPassword { get; set; }
    }

    public sealed record Result
    {
        public required Guid Id { get; set; }
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
                account = await contextIdentity.Accounts
                        .Include(x => x.Features)
                        .Include(x => x.ApiKeys)
                        .FirstOrDefaultAsync(x => x.Id == cmd.AccountId, ctn)
                        ?? throw new IncidentException(IncidentCode.NotFound);

                Validate(account, cmd);

                var createdApiKeys = AccountsPostFeature.Handler.SyncApiKeys(account, cmd.Account.ApiKeys, apiKeyService);

                if (!string.IsNullOrEmpty(cmd.Account.Password))
                {
                    EnsureCurrentPassword(account, cmd.CurrentPassword, hashingService);

                    var hashResult = hashingService.Hash(cmd.Account.Password);
                    account.Password = hashResult.HexHash;
                    account.Salt = hashResult.HexSalt;
                }

                //NOTE: EF will not update the entity, if only the relationships changed
                account.VersionLocal++;

                await contextIdentity.SaveChangesAsync(ctn);

                return new Result { Id = account.Id };
            }
        }

        private static void EnsureCurrentPassword(Account account, string? currentPassword, HashingService hashingService)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                throw new IncidentException(IncidentCode.WrongPassword);
            }

            var presented = hashingService.Hash(currentPassword, account.Salt);

            if (!hashingService.FixedTimeEquals(presented.HexHash, account.Password))
            {
                throw new IncidentException(IncidentCode.WrongPassword);
            }
        }

        private static void Validate(Account account, Command cmd)
        {
            if (account.VersionLocal != cmd.Account.VersionLocal)
            {
                throw new IncidentException(IncidentCode.VersionMismatch);
            }
            if (account.Id != cmd.Account.Id)
            {
                throw new IncidentException(IncidentCode.Unauthorized);
            }
        }
    }
}
