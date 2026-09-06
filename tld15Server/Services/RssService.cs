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
/// The feed the site hands a reader, as RSS 2.0. The document is written here by hand for the same
/// reason <see cref="SitemapService"/> writes the sitemap by hand: the format is a handful of
/// elements, and a syndication library would be a dependency and a reflection surface bought for
/// forty lines of xml.
/// </summary>
/// <remarks>
/// Unlike the sitemap this is built per request rather than once at start. A sitemap that is a
/// deploy behind costs a crawler nothing, because a crawler comes back. A feed that is a deploy
/// behind is a feed that never announced the article, because a reader polls it once and moves on.
/// </remarks>
public static class RssService
{
    /// <summary> What the feed says about itself. </summary>
    /// <param name="Title"> The name of the site. </param>
    /// <param name="Description"> What the site is. </param>
    /// <param name="Language"> The locale the entries below are written in. </param>
    /// <param name="Copyright"> Who the words belong to. </param>
    /// <param name="ImagePath"> The mark of the site, as a path under the web root. </param>
    public sealed record Channel(
        string Title,
        string Description,
        string Language,
        string Copyright,
        string ImagePath);

    /// <summary> One work the feed announces. </summary>
    /// <param name="Path"> The address of the work on this site, with or without its leading slash. </param>
    /// <param name="Title"> The headline. </param>
    /// <param name="Description"> The line under it. </param>
    /// <param name="PosterUrl"> The picture of the work, or nothing when it carries none. </param>
    /// <param name="PublishedAt"> The date the editor gave the work. </param>
    public sealed record Entry(
        string Path,
        string Title,
        string Description,
        string PosterUrl,
        DateTimeOffset PublishedAt);

    private static readonly XNamespace _atom = "http://www.w3.org/2005/Atom";

    /// <summary>
    /// Writes the document. A feed with no entries is still a feed: a reader handed one keeps the
    /// subscription, where one handed a 404 is entitled to drop it.
    /// </summary>
    public static string Build(string origin, Channel channel, IReadOnlyList<Entry> entries)
    {
        var root = origin.Trim().TrimEnd('/');
        var self = $"{root}{Composition.Globals.Route.Rss}";

        // The date the channel last moved is the date of the newest thing in it. With nothing in it
        // there is no such date, and the moment of the answer is the only honest one.
        var built = entries.Count == 0
            ? DateTimeOffset.UtcNow
            : entries.Max(x => x.PublishedAt);

        var items = entries.Select(entry =>
        {
            var address = $"{root}/{entry.Path.TrimStart('/')}";

            return new XElement("item",
                new XElement("title", entry.Title),
                new XElement("link", address),
                // isPermaLink says the id is also somewhere to go. It is: every work is read at its
                // own address, and that address is what tells a reader it has seen this one before.
                new XElement("guid", new XAttribute("isPermaLink", "true"), address),
                new XElement("description", entry.Description),
                Enclosure(root, entry.PosterUrl),
                new XElement("pubDate", Stamp(entry.PublishedAt)));
        });

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "atom", _atom.NamespaceName),
                new XElement("channel",
                    new XElement("title", channel.Title),
                    new XElement("link", $"{root}/"),
                    new XElement("description", channel.Description),
                    new XElement("language", channel.Language),
                    new XElement("copyright", channel.Copyright),
                    new XElement("lastBuildDate", Stamp(built)),
                    new XElement(_atom + "link",
                        new XAttribute("href", self),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", Composition.Globals.Page.Rss.MediaType)),
                    new XElement("image",
                        new XElement("url", $"{root}{channel.ImagePath}"),
                        new XElement("title", channel.Title),
                        new XElement("link", $"{root}/")),
                    items)));

        var builder = new StringBuilder();

        using (var writer = XmlWriter.Create(new Utf8StringWriter(builder), new XmlWriterSettings { Indent = true }))
        {
            document.Save(writer);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The poster of a work as the element a reader draws it from, or nothing when the work carries
    /// no poster. A reader that finds no enclosure draws the item as a line of text.
    /// </summary>
    /// <remarks>
    /// The length is written as zero. RSS asks for the size of the file in bytes, and this site does
    /// not have one to give: a poster is an address on somebody else's host, never an upload, so the
    /// only way to learn its size would be to fetch every poster on every read of the feed. Zero is
    /// what a feed writes when the size is unknown, and every reader treats it as "come and see".
    /// </remarks>
    private static XElement? Enclosure(string root, string posterUrl)
    {
        var url = UrlPolicy.Clean(posterUrl);

        if (url.Length == 0 || !UrlPolicy.IsImageSource(url)) { return null; }

        var address = UrlPolicy.SchemeOf(url).Length == 0
            ? $"{root}/{url.TrimStart('/')}"
            : url;

        return new XElement("enclosure",
            new XAttribute("url", address),
            new XAttribute("length", 0),
            new XAttribute("type", MediaType(address)));
    }

    /// <summary> The media type of a picture, read off the end of its address. </summary>
    /// <remarks>
    /// Read from the address because that is all this site holds. An address that names no format
    /// this site recognises is called a png: the attribute is required, a reader uses it only to
    /// decide how to draw the file, and every one of them sniffs the file it actually received.
    /// </remarks>
    private static string MediaType(string address)
    {
        var path = address.AsSpan();
        var cut = path.IndexOfAny('?', '#');

        if (cut >= 0) { path = path[..cut]; }

        var dot = path.LastIndexOf('.');

        var extension = dot < 0 ? string.Empty : path[(dot + 1)..].ToString().ToLowerInvariant();

        return extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "avif" => "image/avif",
            "svg" => "image/svg+xml",
            _ => "image/png",
        };
    }

    /// <summary>
    /// A date in the form RSS asks for, which is the one RFC 822 named and RFC 1123 tightened. The
    /// "r" format writes GMT, so the value is read the same wherever it lands.
    /// </summary>
    private static string Stamp(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("r", CultureInfo.InvariantCulture);
    }

    private sealed class Utf8StringWriter(StringBuilder builder) : StringWriter(builder)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
