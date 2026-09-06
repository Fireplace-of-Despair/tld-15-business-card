// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;
using StainlessCore.Exceptions;
using StainlessCore.Features;
using StainlessCore.Service;
using StainlessInfrastructure;
using StainlessInfrastructure.Models.Identity;
using tld15Server.Composition;
using tld15Server.Features.Shared.System;
using tld15Server.Services;

namespace tld15Server.Features.Identity;

public sealed class IdentityPostFeature : IFeature
{
    public const string Id = "identity.post";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<SharedSession>
    {
        public required string Login { get; set; }
        public required string Password { get; set; }
        public required string? UserAgent { get; set; }
        public required string? UserIP { get; set; }
        public required string? AcceptLanguage { get; set; }
        public required string? AcceptEncoding { get; set; }
    }

    public sealed class Handler(
          IConfiguration configuration
        , IDbContextFactory<DataContextIdentity> contextIdentityFactory
        , HashingService hashingService
        , CacheManager cacheManager
        , LoginThrottle loginThrottle
        ) : ICommandHandler<Command, SharedSession>
    {
        // Salt used to hash the password of a login that does not exist. Hashing anyway costs the same
        // as the real path, so response time no longer reveals whether the account is there.
        private const string AbsentAccountSalt = "00000000000000000000000000000000";

        public async ValueTask<SharedSession> Handle(Command command, CancellationToken ctn)
        {
            Log.Information("Login attempt from {UserIP}", command.UserIP);
            loginThrottle.EnsureNotLockedOut(command.Login, command.UserIP);

            await Task.Delay(500, ctn);
            await CreateFirstUserAsync(command, ctn);

            SharedSession result;
            try
            {
                result = await GetFeatures(command, ctn);
            }
            catch (IncidentException)
            {
                loginThrottle.RegisterFailure(command.Login, command.UserIP);
                throw;
            }

            loginThrottle.RegisterSuccess(command.Login, command.UserIP);
            return result;
        }

        private async Task CreateFirstUserAsync(Command command, CancellationToken ctn)
        {
            for (var attempt = 0; attempt <= 5; attempt++)
            {
                try
                {
                    using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
                    {
                        await using var transaction = await contextIdentity.Database.BeginTransactionAsync(IsolationLevel.Serializable, ctn);

                        var anyAccounts = await contextIdentity.Accounts
                            .AsNoTracking()
                            .Where(x => x.AccountTypeId != Globals.Reference.AccountType.Automation)
                            .AnyAsync(ctn);

                        if (anyAccounts)
                        {
                            await transaction.CommitAsync(ctn);
                            return;
                        }

                        var hasResult = hashingService.Hash(command.Password);

                        var allFeatures = await contextIdentity.Features
                            .ToListAsync(ctn);

                        var account = new Account
                        {
                            Id = Guid.NewGuid(),
                            Login = command.Login,
                            Password = hasResult.HexHash,
                            Salt = hasResult.HexSalt,
                            AccountTypeId = Globals.Reference.AccountType.User,
                            AccountStatusId = Globals.Reference.AccountStatus.Enabled,
                        };
                        foreach (var item in allFeatures)
                        {
                            account.Features.Add(item);
                        }

                        await contextIdentity.Accounts.AddAsync(account, ctn);
                        await contextIdentity.SaveChangesAsync(ctn);
                        await transaction.CommitAsync(ctn);
                    }
                }
                catch (Exception ex) when (IsSerializationFailure(ex))
                {
                    Log.Error(ex, "CreateFirstUserAsync: transaction race");

                    if (attempt == 5) { throw; }

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(10 * (attempt + 1)),
                        ctn);
                }
            }
        }

        private async Task<SharedSession> GetFeatures(Command command, CancellationToken ctn)
        {
            using (var contextIdentity = await contextIdentityFactory.CreateDbContextAsync(ctn))
            {
                var account = await contextIdentity.Accounts
                .AsNoTracking()
                .Where(x =>
                       x.Login == command.Login
                    && x.AccountStatusId == Globals.Reference.AccountStatus.Enabled)
                .Select(x => new
                {
                    x.Id,
                    x.Login,
                    x.Password,
                    x.Salt,
                    Features = x.Features.Select(f => f.Id).ToList()
                })
                .FirstOrDefaultAsync(ctn);

                // An unknown login and a wrong password must be indistinguishable: same work, same
                // incident. Reporting NotFound here would confirm which logins exist.
                if (account == null)
                {
                    hashingService.Hash(command.Password, AbsentAccountSalt);
                    throw new IncidentException(IncidentCode.WrongPassword);
                }

                var hasResult = hashingService.Hash(command.Password, account.Salt);
                if (!hashingService.FixedTimeEquals(hasResult.HexHash, account.Password))
                {
                    throw new IncidentException(IncidentCode.WrongPassword);
                }

                if (!account.Features.Contains(FeatureId))
                {
                    throw new IncidentException(IncidentCode.Forbidden);
                }

                var experationMinutes = configuration.GetValue<int>(Globals.Security.CookieExpiration);
                var session = new SharedSession
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    Login = account.Login,
                    Features = account.Features,
                    UserAgent = command.UserAgent,
                    UserIP = command.UserIP,
                    AcceptEncoding = command.AcceptEncoding,
                    AcceptLanguage = command.AcceptLanguage,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(experationMinutes),
                };
                cacheManager.Save(session, session.ExpiresAt);

                return session;
            }
        }

        private static bool IsSerializationFailure(Exception? ex)
        {
            while (ex != null)
            {
                if (ex is PostgresException pg &&
                    pg.SqlState == PostgresErrorCodes.SerializationFailure)
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }
    }
}
