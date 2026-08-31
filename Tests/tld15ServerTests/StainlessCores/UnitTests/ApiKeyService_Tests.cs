// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using StainlessCore.Service;

namespace tld15ServerTests.StainlessCores.UnitTests;

[Trait("Category", "StainlessCore")]
public class ApiKeyService_Tests
{
    private static ApiKeyService CreateService(string pepper = "test-pepper")
    {
        var appSettings = $@"{{""Security"": {{ ""Pepper"": ""{pepper}"" }} }}";

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appSettings)))
            .Build();

        return new ApiKeyService(configuration);
    }

    [Fact]
    public void Generate_ProducesAFreshKeyEveryTime()
    {
        var keys = Enumerable.Range(0, 100).Select(_ => ApiKeyService.Generate()).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
    }

    [Fact]
    public void Generate_IsUrlAndHeaderSafe()
    {
        var key = ApiKeyService.Generate();

        // base64url of 32 bytes, padding trimmed
        Assert.Equal(43, key.Length);
        Assert.DoesNotContain('+', key);
        Assert.DoesNotContain('/', key);
        Assert.DoesNotContain('=', key);
    }

    [Fact]
    public void Hash_IsStable_SoALookupCanFindTheKey()
    {
        var service = CreateService();
        var key = ApiKeyService.Generate();

        Assert.Equal(service.Hash(key), service.Hash(key));
    }

    [Fact]
    public void Hash_DiffersPerKey()
    {
        var service = CreateService();

        Assert.NotEqual(service.Hash(ApiKeyService.Generate()), service.Hash(ApiKeyService.Generate()));
    }

    // The hash is what lands in the database; it must not carry the key itself.
    [Fact]
    public void Hash_DoesNotContainTheKey()
    {
        var service = CreateService();
        var key = ApiKeyService.Generate();

        var hash = service.Hash(key);

        Assert.DoesNotContain(key, hash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(key, hash);
    }

    // Pepper is part of the hash, so a stolen database alone does not let an attacker precompute hashes.
    [Fact]
    public void Hash_DependsOnThePepper()
    {
        var key = ApiKeyService.Generate();

        Assert.NotEqual(CreateService("pepper-one").Hash(key), CreateService("pepper-two").Hash(key));
    }
}
