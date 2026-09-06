// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace tld15Server.Services;

/// <summary>
/// The sitemap the site hands a crawler, held as one string. It is written once, at start, and read
/// from memory afterwards: a crawler asks for it rarely and a query per read would be paid for out
/// of the same server every visitor is waiting on.
/// </summary>
/// <remarks>
/// The document therefore describes the works the site carried when it started. A work published
/// after that appears in the sitemap on the next start — a deploy, or a restart.
/// </remarks>
public sealed class SitemapService
{
    /// <summary> One address the sitemap points a crawler at. </summary>
    /// <param name="Path"> The path on this site, with or without its leading slash. </param>
    /// <param name="LastModified"> When what sits at that address last moved. </param>
    public sealed record Entry(string Path, DateTimeOffset LastModified);

    private static readonly XNamespace _namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary> The document, or an empty string while the site does not know its own address. </summary>
    public string Xml { get; private set; } = string.Empty;

    /// <summary>
    /// Writes the document. Without a host there is nothing to write: a sitemap is made of absolute
    /// addresses, and a relative one is not something a crawler will follow.
    /// </summary>
    public void Build(string? origin, IReadOnlyList<Entry> entries)
    {
        if (string.IsNullOrWhiteSpace(origin) || entries.Count == 0)
        {
            Xml = string.Empty;
            return;
        }

        var root = origin.Trim().TrimEnd('/');

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(_namespace + "urlset", entries.Select(entry => new XElement(
                _namespace + "url",
                new XElement(_namespace + "loc", $"{root}/{entry.Path.TrimStart('/')}"),
                new XElement(_namespace + "lastmod", Stamp(entry.LastModified))))));

        var builder = new StringBuilder();

        // The declaration has to say utf-8, which it only does when the writer is asked in what the
        // response will be encoded rather than in what a StringBuilder happens to hold.
        using (var writer = XmlWriter.Create(new Utf8StringWriter(builder), new XmlWriterSettings { Indent = true }))
        {
            document.Save(writer);
        }

        Xml = builder.ToString();
    }

    /// <summary> A date in the form the sitemap schema asks for. </summary>
    private static string Stamp(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed class Utf8StringWriter(StringBuilder builder) : StringWriter(builder)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
