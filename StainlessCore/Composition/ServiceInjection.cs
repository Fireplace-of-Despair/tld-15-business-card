// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StainlessCore.Service;
using IPNetwork = System.Net.IPNetwork;

namespace StainlessCore.Composition;

/// <summary> Dependency-injection wiring of the shared Core services </summary>
public static class ServiceInjection
{
    /// <summary> Inject Core services. If <see cref="IMemoryCache"/> is not injected, it should before </summary>
    /// <param name="builder"><see cref="IHostApplicationBuilder"/></param>
    /// <returns><see cref="IHostApplicationBuilder"/></returns>
    public static IHostApplicationBuilder InjectCore(this IHostApplicationBuilder builder)
    {
        // LoginThrottle counts through IMemoryCache. AddMemoryCache is idempotent, so a host that
        // already registered the cache is unaffected.
        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<HashingService>();
        builder.Services.AddSingleton<ApiKeyService>();
        builder.Services.AddSingleton<LoginThrottle>();

        return builder;
    }

    /// <summary> Bind <see cref="ForwardedHeadersOptions"/> from the trusted-proxy settings </summary>
    /// <param name="builder"><see cref="IHostApplicationBuilder"/></param>
    /// <param name="configuration"><see cref="IConfiguration"/> that holds the proxy settings</param>
    /// <returns><see cref="IHostApplicationBuilder"/></returns>
    public static IHostApplicationBuilder AddForwardedHeaders(this IHostApplicationBuilder builder, IConfiguration configuration)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            var forwardLimit = configuration.GetValue<int>(Globals.Security.ForwardedHeadersForwardLimit)!;
            if (forwardLimit > 0)
            {
                options.ForwardLimit = forwardLimit;
            }

            var knownProxies = ReadEntries(configuration, Globals.Security.ForwardedHeadersKnownProxies);
            var knownNetworks = ReadEntries(configuration, Globals.Security.ForwardedHeadersKnownNetworks);

            // X-Forwarded-For is caller-supplied, so it is only honoured from proxies named here. With nothing
            // configured the framework defaults stand (loopback only), which is why an unconfigured deployment
            // keeps using the real connection address instead of trusting a spoofable header.
            if (knownProxies.Length == 0 && knownNetworks.Length == 0)
            {
                return;
            }

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(IPNetwork.Parse(network));
            }
        });

        return builder;
    }

    /// <summary> Read a configuration array of text entries. It drops an empty entry. </summary>
    /// <param name="configuration"><see cref="IConfiguration"/> to read</param>
    /// <param name="key">Key of the array</param>
    /// <returns>The entries, or an empty array</returns>
    /// <remarks>
    /// This reads the children by hand instead of calling <c>Get&lt;string[]&gt;()</c>. The binder needs
    /// reflection, which does not survive trimming, and this project sets PublishTrimmed. The binder
    /// source generator is no answer either: it emits <c>List&lt;string&gt;</c> without the matching
    /// using, so it does not compile while ImplicitUsings stays off.
    /// </remarks>
    private static string[] ReadEntries(IConfiguration configuration, string key)
    {
        return [.. configuration
            .GetSection(key)
            .GetChildren()
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Value!)];
    }
}
