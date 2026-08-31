// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using Microsoft.AspNetCore.Http;
using tld15Server.Features.Shared.System;

namespace tld15Server.Frontend;

/// <summary>
/// The request metadata a session is pinned to. A session survives only while the caller keeps
/// looking like the caller that opened it, and both places that check this read the rule from here.
/// </summary>
internal static class SessionMetadata
{
    /// <summary>
    /// Describes the first field of the request that no longer matches the session, or null while
    /// the request still looks like the one the session was opened with.
    /// </summary>
    internal static string? Mismatch(HttpContext? context, SharedSession session)
    {
        if (context == null)
        {
            return "the caller carries no http context";
        }

        var request = context.Request;

        return Compare("user agent", session.UserAgent, request.Headers.UserAgent.ToString())
            ?? Compare("address", session.UserIP, context.Connection.RemoteIpAddress?.ToString())
            ;
    }

    private static string? Compare(string name, string? stored, string? received)
    {
        if (string.Equals(stored, received, StringComparison.Ordinal))
        {
            return null;
        }

        return $"the {name} changed: the session was opened with '{stored}', the request carries '{received}'";
    }
}
