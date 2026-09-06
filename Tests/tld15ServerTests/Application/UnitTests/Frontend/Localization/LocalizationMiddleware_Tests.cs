// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using tld15Server.Composition;
using tld15Server.Frontend.Localization;

namespace tld15ServerTests.Application.UnitTests.Frontend.Localization;

[Trait("Application", "Unit Tests")]
public class LocalizationMiddleware_Tests
{
    private static readonly string _defaultLanguage = Globals.Locales.First().Key;

    private static DefaultHttpContext CreateContext(string? languageCookie)
    {
        var context = new DefaultHttpContext();

        if (languageCookie != null)
        {
            context.Request.Headers.Cookie = $"{Globals.Cookie.Language}={languageCookie}";
        }

        return context;
    }

    private static async Task<(CultureInfo Culture, CultureInfo UICulture)> RunAsync(string? languageCookie)
    {
        CultureInfo? seen = null;
        CultureInfo? seenUI = null;

        var middleware = new LocalizationMiddleware(_ =>
        {
            seen = CultureInfo.CurrentCulture;
            seenUI = CultureInfo.CurrentUICulture;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(CreateContext(languageCookie));

        Assert.NotNull(seen);
        Assert.NotNull(seenUI);
        return (seen, seenUI);
    }

    [Fact]
    public async Task Culture_ComesFromTheLanguageCookie()
    {
        var (culture, uiCulture) = await RunAsync("ja");

        Assert.Equal("ja", culture.Name);
        Assert.Equal("ja", uiCulture.Name);
    }

    [Theory]
    [InlineData(null)]          // no cookie at all
    [InlineData("")]            // empty cookie
    [InlineData("de")]          // a real culture the app does not support
    [InlineData("not-a-locale")] // junk that would throw if handed to CultureInfo directly
    public async Task UnsupportedLanguage_FallsBackToTheDefault(string? languageCookie)
    {
        var (culture, uiCulture) = await RunAsync(languageCookie);

        Assert.Equal(_defaultLanguage, culture.Name);
        Assert.Equal(_defaultLanguage, uiCulture.Name);
    }

    // The regression that mattered: the middleware used to assign CultureInfo.DefaultThreadCurrent*,
    // which is process-wide state. One visitor's language then leaked into every other request.
    // The defaults are cleared first on purpose - comparing against whatever they happen to hold
    // passes even with the bug present, because a previous request already set them to the same value.
    [Fact]
    public async Task Culture_DoesNotLeakIntoProcessWideDefaults()
    {
        var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
        var previousUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            CultureInfo.DefaultThreadCurrentCulture = null;
            CultureInfo.DefaultThreadCurrentUICulture = null;

            await RunAsync("ja");

            Assert.Null(CultureInfo.DefaultThreadCurrentCulture);
            Assert.Null(CultureInfo.DefaultThreadCurrentUICulture);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUICulture;
        }
    }

    // Two requests overlapping in one process must not see each other's language.
    [Fact]
    public async Task ConcurrentRequests_KeepTheirOwnCulture()
    {
        var japanese = RunAsync("ja");
        var fallback = RunAsync("de");

        var results = await Task.WhenAll(japanese, fallback);

        Assert.Equal("ja", results[0].Culture.Name);
        Assert.Equal(_defaultLanguage, results[1].Culture.Name);
    }
}
