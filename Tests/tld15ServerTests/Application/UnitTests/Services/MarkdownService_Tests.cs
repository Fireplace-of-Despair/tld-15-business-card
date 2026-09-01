// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using tld15Server.Services;

namespace tld15ServerTests.Application.UnitTests.Services;

[Trait("Application", "Unit Tests")]
public class MarkdownService_Tests
{
    private readonly MarkdownService _service = new();

    #region Rendering

    [Fact]
    public void ToHtml_RendersHeadingsAndProse()
    {
        var html = _service.ToHtml("# Title\n\nA line of *prose*.");

        Assert.Contains("<h1", html, StringComparison.Ordinal);
        Assert.Contains("<em>prose</em>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_RendersGithubTables()
    {
        var html = _service.ToHtml("| a | b |\n| - | - |\n| 1 | 2 |");

        Assert.Contains("<table>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_RendersGithubStrikethrough()
    {
        var html = _service.ToHtml("~~gone~~");

        Assert.Contains("<del>gone</del>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void ToHtml_ReturnsEmpty_WhenThereIsNothingToRender(string? markdown)
    {
        Assert.Equal(string.Empty, _service.ToHtml(markdown));
    }

    [Fact]
    public void ToHtml_ReturnsTheSameInstance_WhenTheTextRepeats()
    {
        const string markdown = "# Cached";

        Assert.Same(_service.ToHtml(markdown), _service.ToHtml(markdown));
    }

    #endregion

    #region Safety

    [Fact]
    public void ToHtml_EscapesRawHtml()
    {
        var html = _service.ToHtml("<script>alert(1)</script>");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_EscapesRawHtmlInsideAParagraph()
    {
        var html = _service.ToHtml("a line with <img src=x onerror=alert(1)> in it");

        // The handler survives as the text of a handler, which is the point: it is words on a page,
        // not an attribute a browser reads.
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_KeepsTheText_AndDropsTheAddress_WhenTheSchemeIsNotAllowed()
    {
        var html = _service.ToHtml("[press me](javascript:alert(1))");

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("press me", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[x](JaVaScRiPt:alert(1))")]
    [InlineData("[x](java\tscript:alert(1))")]
    [InlineData("<javascript:alert(1)>")]
    [InlineData("![x](data:text/html;base64,PHNjcmlwdD4=)")]
    public void ToHtml_DropsEveryAddressOffTheAllowedSchemes(string markdown)
    {
        var html = _service.ToHtml(markdown);

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToHtml_SendsAnOutwardLinkToItsOwnTab()
    {
        var html = _service.ToHtml("[site](https://example.org)");

        Assert.Contains("href=\"https://example.org\"", html, StringComparison.Ordinal);
        Assert.Contains("target=\"_blank\"", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"noopener noreferrer\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_KeepsALinkInsideTheSiteInTheSameTab()
    {
        var html = _service.ToHtml("[home](/#lore)");

        Assert.Contains("href=\"/#lore\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("target=", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_KeepsAMailAddress()
    {
        var html = _service.ToHtml("[write](mailto:someone@example.org)");

        Assert.Contains("mailto:someone@example.org", html, StringComparison.Ordinal);
    }

    #endregion
}
