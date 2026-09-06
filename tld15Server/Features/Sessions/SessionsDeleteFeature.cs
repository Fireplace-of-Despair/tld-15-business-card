// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using StainlessCore.Features;
using tld15Server.Services;

namespace tld15Server.Features.Sessions;

public sealed class SessionsDeleteFeature : IFeature
{
    public const string Id = "sessions.delete";
    public static string FeatureId => Id;

    public sealed record Command : ICommand<bool>
    {
        public required Guid Id { get; set; }
    }

    public sealed class Handler(CacheManager cacheManager) : ICommandHandler<Command, bool>
    {
        public async ValueTask<bool> Handle(Command command, CancellationToken ctn)
        {
            await Task.Delay(10, ctn);

            cacheManager.RemoveSessionById(command.Id);
            return true;
        }
    }
}
