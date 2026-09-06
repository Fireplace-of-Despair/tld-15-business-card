// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using tld15Server.Common;

namespace tld15ServerTests.Application.UnitTests.Frontend.Components;

/// <summary>
/// The icon a button draws now follows from the address the link carries, so these cover the
/// addresses the site actually stores rather than the shape of the table behind them.
/// </summary>
[Trait("Application", "Unit Tests")]
public class IconHelper_Tests
{
    [Theory]
    [InlineData("https://github.com/ChiefNoir/ss-tgfmb", "github")]
    [InlineData("https://www.amazon.com/dp/B08THQHF4C", "amazon")]
    [InlineData("https://www.pixiv.net/user/976281/series/5220", "pixiv")]
    [InlineData("https://www.royalroad.com/fiction/51705/acroamatic-security-pocket-reference", "royalroad")]
    [InlineData("https://steamcommunity.com/workshop/filedetails/?id=2366451966", "steam")]
    [InlineData("https://www.youtube.com/@chiefnoir", "youtube")]
    [InlineData("https://www.linkedin.com/in/stshevtsov/", "linkedin")]
    [InlineData("https://t.me/stainless_chief", "telegram")]
    public void GetNameByUrl_ReadsTheHost(string url, string expected)
    {
        Assert.Equal(expected, IconHelper.GetNameByUrl(url));
    }

    [Fact]
    public void GetNameByUrl_ReadsASubdomainAsItsSite()
    {
        Assert.Equal("itch", IconHelper.GetNameByUrl("https://fireplace-of-despair.itch.io/acroamatic-security-notes"));
    }

    [Fact]
    public void GetNameByUrl_ReadsAFeedFromItsPath()
    {
        // The feed sits on the site's own host, so nothing but the path tells it apart.
        Assert.Equal("rss", IconHelper.GetNameByUrl("https://fireplace-of-despair.org/rss"));
    }

    [Fact]
    public void GetNameByUrl_ReadsAMailAddressFromItsScheme()
    {
        Assert.Equal("email", IconHelper.GetNameByUrl("mailto:someone@example.org"));
    }

    [Theory]
    [InlineData("https://storage.fireplace-of-despair.org/vhd-11.zip")]
    [InlineData("https://storage.fireplace-of-despair.org/void-harmonization-division/vhd-12-chief.mp3")]
    public void GetNameByUrl_ReadsWhatTheSiteHandsOutItself(string url)
    {
        Assert.Equal("pirate", IconHelper.GetNameByUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/projects/tld-15")]
    [InlineData("https://ridero.ru/books/marshruty_tryokhmernogo_prostranstva/")]
    public void GetNameByUrl_NamesNothingItDoesNotRecognise(string? url)
    {
        Assert.Equal(string.Empty, IconHelper.GetNameByUrl(url));
    }

    [Fact]
    public void GetIconByUrl_FallsBackToThePlaceholder()
    {
        // An address the table says nothing about still draws a button; it draws the unknown icon.
        Assert.NotNull(IconHelper.GetIconByUrl("https://example.org/somewhere"));
    }
}
