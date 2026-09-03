// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System.Text;
using tld15Server.Composition;

namespace tld15Server.Services;

/// <summary>
/// The robots.txt the site hands a crawler. It is composed here instead of sitting as a file in the
/// web root, and one line is the reason: the address of the sitemap has to be absolute, the
/// specification allows nothing else, and the only thing that knows the address of this site is the
/// configuration. A file would have to carry a second copy of that host and would go stale the day
/// the site moved.
/// </summary>
public static class RobotsService
{
    /// <summary>
    /// Writes the document for a given origin. Without one the sitemap line is left out rather than
    /// written relative: a crawler follows an absolute address or it follows nothing.
    /// </summary>
    public static string Build(string? origin)
    {
        var builder = new StringBuilder();

        builder.AppendLine("User-agent: *");
        builder.AppendLine();
        builder.AppendLine("# Everything that manages the site sits under one segment, so one line covers every page of");
        builder.AppendLine("# it - including the ones added after this file was last opened.");
        builder.AppendLine($"Disallow: {Globals.Route.Admin}/");
        builder.AppendLine();
        builder.AppendLine("# The door, and the act of closing it. Neither is a page anybody should arrive at from a");
        builder.AppendLine("# search.");
        builder.AppendLine($"Disallow: {Globals.Route.Identity}/");

        if (!string.IsNullOrWhiteSpace(origin))
        {
            builder.AppendLine();
            builder.AppendLine("# The one address a crawler is asked to read on its own.");
            builder.AppendLine($"Sitemap: {origin.Trim().TrimEnd('/')}{Globals.Route.Sitemap}");
        }

        return builder.ToString();
    }
}
