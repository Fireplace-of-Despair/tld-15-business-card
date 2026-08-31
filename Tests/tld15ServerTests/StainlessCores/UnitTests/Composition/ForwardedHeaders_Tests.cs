// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StainlessCore.Composition;

namespace tld15ServerTests.StainlessCores.UnitTests.Composition;

[Trait("Category", "StainlessCore")]
public class ForwardedHeaders_Tests
{
    private static IConfiguration CreateConfiguration(string json)
    {
        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
    }

    // AddForwardedHeaders is the only entry point, so the options come back through the container it
    // configured. An empty host keeps ambient settings files out of the result.
    private static ForwardedHeadersOptions Configure(string json)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.AddForwardedHeaders(CreateConfiguration(json));

        return builder.Build().Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void ClientAddressAndScheme_AreBothForwarded()
    {
        var options = Configure("{}");

        Assert.True(options.ForwardedHeaders.HasFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto));
    }

    // X-Forwarded-For is attacker-controlled. Trusting it from anywhere would let a caller invent a new
    // client address per request, which defeats both the per-address sign-in lockout and the session
    // metadata check. With nothing configured the framework defaults (loopback only) must survive.
    [Fact]
    public void WithoutConfiguration_OnlyLoopbackStaysTrusted()
    {
        var defaults = new ForwardedHeadersOptions();
        var options = Configure("{}");

        Assert.Equal(defaults.KnownProxies.Count, options.KnownProxies.Count);
        Assert.Equal(defaults.KnownIPNetworks.Count, options.KnownIPNetworks.Count);
        Assert.Contains(options.KnownProxies, x => IPAddress.IsLoopback(x));
    }

    [Fact]
    public void ConfiguredProxies_ReplaceTheDefaults()
    {
        var options = Configure(@"{ ""Security"": { ""ForwardedHeaders"": {
            ""KnownProxies"": [ ""10.1.2.3"", ""192.168.0.7"" ] } } }");

        Assert.Equal(2, options.KnownProxies.Count);
        Assert.Contains(IPAddress.Parse("10.1.2.3"), options.KnownProxies);
        Assert.Contains(IPAddress.Parse("192.168.0.7"), options.KnownProxies);
        Assert.DoesNotContain(options.KnownProxies, x => IPAddress.IsLoopback(x));
    }

    [Fact]
    public void ConfiguredNetworks_AreParsedAsCidr()
    {
        var options = Configure(@"{ ""Security"": { ""ForwardedHeaders"": {
            ""KnownNetworks"": [ ""10.0.0.0/8"" ] } } }");

        var network = Assert.Single(options.KnownIPNetworks);
        Assert.Equal(IPAddress.Parse("10.0.0.0"), network.BaseAddress);
        Assert.Equal(8, network.PrefixLength);
        Assert.True(network.Contains(IPAddress.Parse("10.4.5.6")));
        Assert.False(network.Contains(IPAddress.Parse("172.16.0.1")));
    }

    [Fact]
    public void ForwardLimit_IsTakenFromConfiguration()
    {
        var options = Configure(@"{ ""Security"": { ""ForwardedHeaders"": { ""ForwardLimit"": 3 } } }");

        Assert.Equal(3, options.ForwardLimit);
    }

    [Fact]
    public void ForwardLimit_KeepsTheDefaultWhenUnset()
    {
        var defaults = new ForwardedHeadersOptions();
        var options = Configure("{}");

        Assert.Equal(defaults.ForwardLimit, options.ForwardLimit);
    }
}
