// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using StainlessCore.Common.Helpers;

namespace tld15ServerTests.StainlessCores.UnitTests.Common.Helpers;

[Trait("Category", "StainlessCore")]
public class HttpContextAccessorHelper_Tests
{
    private const string ErrorText = "ERROR CORE";

    private static HttpContextAccessor CreateHttpContextAccessor(
        string? userAgent = null,
        string? acceptLanguage = null,
        string? acceptEncoding = null,
        string? remoteIp = null)
    {
        var context = new DefaultHttpContext();

        if (userAgent != null)
        {
            context.Request.Headers.UserAgent = new StringValues(userAgent);
        }

        if (acceptLanguage != null)
        {
            context.Request.Headers.AcceptLanguage = new StringValues(acceptLanguage);
        }

        if (acceptEncoding != null)
        {
            context.Request.Headers.AcceptEncoding = new StringValues(acceptEncoding);
        }

        if (remoteIp != null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }

        return new HttpContextAccessor
        {
            HttpContext = context
        };
    }

    [Fact]
    public void GetUserAgent_ReturnsUserAgent_WhenHeaderExists()
    {
        var accessor = CreateHttpContextAccessor(userAgent: "Mozilla/5.0");

        var result = accessor.GetUserAgent();

        Assert.Equal("Mozilla/5.0", result);
    }

    [Fact]
    public void GetUserAgent_ReturnsError_WhenHttpContextIsNull()
    {
        var accessor = new HttpContextAccessor();

        var result = accessor.GetUserAgent();

        Assert.Equal(ErrorText, result);
    }

    [Fact]
    public void GetRemoteIpAddress_ReturnsIp_WhenIpExists()
    {
        var accessor = CreateHttpContextAccessor(remoteIp: "127.0.0.1");

        var result = accessor.GetRemoteIpAddress();

        Assert.Equal("127.0.0.1", result);
    }

    [Fact]
    public void GetRemoteIpAddress_ReturnsError_WhenIpIsNull()
    {
        var accessor = CreateHttpContextAccessor();

        var result = accessor.GetRemoteIpAddress();

        Assert.Equal(ErrorText, result);
    }

    [Fact]
    public void GetAcceptLanguage_ReturnsHeader_WhenExists()
    {
        var accessor = CreateHttpContextAccessor(acceptLanguage: "en-US");

        var result = accessor.GetAcceptLanguage();

        Assert.Equal("en-US", result);
    }

    [Fact]
    public void GetAcceptLanguage_ReturnsEmptyString_WhenHeaderMissing()
    {
        var accessor = CreateHttpContextAccessor();

        var result = accessor.GetAcceptLanguage();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetAcceptEncoding_ReturnsHeader_WhenExists()
    {
        var accessor = CreateHttpContextAccessor(acceptEncoding: "gzip");

        var result = accessor.GetAcceptEncoding();

        Assert.Equal("gzip", result);
    }

    [Fact]
    public void GetAcceptEncoding_ReturnsEmptyString_WhenHeaderMissing()
    {
        var accessor = CreateHttpContextAccessor();

        var result = accessor.GetAcceptEncoding();

        Assert.Equal(string.Empty, result);
    }
}
