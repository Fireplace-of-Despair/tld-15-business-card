// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Linq;
using System.Xml.Linq;
using tld15Server.Services;

namespace tld15ServerTests.Application.UnitTests.Services;

[Trait("Application", "Unit Tests")]
public class SitemapService_Tests
{
    private static readonly DateTimeOffset _moment = new(2019, 5, 4, 21, 30, 0, TimeSpan.Zero);

    private readonly SitemapService _service = new();

    private static SitemapService.Entry[] TwoEntries() =>
    [
        new("/", _moment),
        new("/projects/tld-15", _moment),
    ];

    [Fact]
    public void Build_WritesOneUrlPerEntry_AsAnAbsoluteAddress()
    {
        _service.Build("https://example.org", TwoEntries());

        var document = XDocument.Parse(_service.Xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locations = document.Root!.Elements(ns + "url").Select(x => x.Element(ns + "loc")!.Value).ToList();

        Assert.Equal(["https://example.org/", "https://example.org/projects/tld-15"], locations);
    }

    [Fact]
    public void Build_WritesTheDateInTheFormTheSchemaAsksFor()
    {
        _service.Build("https://example.org", TwoEntries());

        Assert.Contains("<lastmod>2019-05-04</lastmod>", _service.Xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SaysTheDocumentIsUtf8()
    {
        _service.Build("https://example.org", TwoEntries());

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", _service.Xml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://example.org/")]
    [InlineData("  https://example.org  ")]
    public void Build_JoinsTheHostAndThePathOnOneSlash(string origin)
    {
        _service.Build(origin, [new("/projects/tld-15", _moment)]);

        Assert.Contains("<loc>https://example.org/projects/tld-15</loc>", _service.Xml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WritesNothing_WhenTheSiteDoesNotKnowItsOwnAddress(string? origin)
    {
        _service.Build(origin, TwoEntries());

        Assert.Equal(string.Empty, _service.Xml);
    }

    [Fact]
    public void Build_WritesNothing_WhenThereIsNoAddressToPointAt()
    {
        _service.Build("https://example.org", []);

        Assert.Equal(string.Empty, _service.Xml);
    }
}
