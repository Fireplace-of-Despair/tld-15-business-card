// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Composition;

internal static class Globals
{
    public static class Error
    {
        public const string Text = "ERROR CORE";
    }

    public static class Cache
    {
        public const string LoginAttemptsByLoginPrefix = "login-attempts:login:";
        public const string LoginAttemptsByAddressPrefix = "login-attempts:address:";
    }

    public static class Security
    {
        /// <summary> Secret that every hash mixes in. It stays outside the database. </summary>
        public const string Pepper = "Security:Pepper";

        /// <summary> Salt length in bytes for a password hash </summary>
        public const string SaltSize = "Security:SaltSize";

        /// <summary> Failed sign-ins allowed per login before it is locked out </summary>
        public const string LoginMaxAttempts = "Security:LoginMaxAttempts";

        /// <summary> Failed sign-ins allowed per source address before it is locked out </summary>
        public const string LoginMaxAttemptsPerAddress = "Security:LoginMaxAttemptsPerAddress";

        /// <summary> How long a lockout lasts, and the window failures are counted over </summary>
        public const string LoginLockoutMinutes = "Security:LoginLockoutMinutes";

        /// <summary> Individual reverse proxies whose forwarded headers are trusted </summary>
        public const string ForwardedHeadersKnownProxies = "Security:ForwardedHeaders:KnownProxies";

        /// <summary> Networks (CIDR) whose forwarded headers are trusted </summary>
        public const string ForwardedHeadersKnownNetworks = "Security:ForwardedHeaders:KnownNetworks";

        /// <summary> How many proxy hops to walk back through the forwarded header chain </summary>
        public const string ForwardedHeadersForwardLimit = "Security:ForwardedHeaders:ForwardLimit";
    }
}
