// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using System;
using System.Collections.Generic;
using System.Linq;

namespace tld15Server.Composition;

public static class Globals
{
    public static string Error => "ERROR SERVER";

    internal static class Divisions
    {
        /// <summary> Fireplace of Despair (Main) </summary>
        internal const string FOD = "FOD";

        /// <summary> Ashen Chronicles Division (Archive)</summary>
        internal const string ACD = "ACD";

        /// <summary> Double Standards Division (Public Relation and Ethics)</summary>
        internal const string DSD = "DSD";

        /// <summary> Fractured Lens Division (Video Production) </summary>
        internal const string FLD = "FLD";

        /// <summary> Inkwell Reverie Division (Writing)</summary>
        internal const string IRD = "IRD";

        /// <summary> Omnia Constructs Division (IRL Production)</summary>
        internal const string OCD = "OCD";

        /// <summary> Obscure Esoteric Division (Lore and Knowledge)</summary>
        internal const string OED = "OED";

        /// <summary> Stellar Logistics Division (Logistics)</summary>
        internal const string SLD = "SLD";

        /// <summary> Stellar Sky Division (Does not exist)</summary>
        internal const string SSD = "SSD";

        /// <summary> Tamed Logic Division (IT)</summary>
        internal const string TLD = "TLD";

        /// <summary> Void Harmonization Division (Music Production)</summary>
        internal const string VHD = "VHD";

        /// <summary> Enclave Solutions Division (Enclave Solutions)</summary>
        internal const string ESD = "ESD";
    }

    /// <summary> The ids the blocks of the front page carry, which its local links jump to. </summary>
    /// <remarks> A block that stands for a content takes the id of that content instead. </remarks>
    public static class Anchor
    {
        public static string Articles => "articles";
        public static string Projects => "projects";
    }

    /// <summary> The pictures the site owns, as paths under the web root. </summary>
    public static class Image
    {
        /// <summary> 1200x630, the picture a share card shows when there is no work to show. </summary>
        public const string ShareCard = "/images/logo_card.png";
        public const int ShareCardWidth = 1200;
        public const int ShareCardHeight = 630;
        public const string ShareCardType = "image/png";
        public const string Logo = "/images/logo_touch.png";
    }

    public static class Route
    {
        public const string Admin = "/admin";

        public const string Identity = "/identity";
        public const string Sitemap = "/sitemap.xml";
        public const string Robots = "/robots.txt";
        public const string Rss = "/rss";
        public const string NotFound = "/404";
    }

    public static class ProjectType
    {
        public static string Project => "project";
        public static string Article => "article";
    }

    public static class Project
    {
        /// <summary> The width of <c>business.project.id</c>, which an editor must not overrun. </summary>
        public const int IdMaxLength = 64;

        /// <summary> The shape of an id, matching the check the reference tables carry. </summary>
        public const string IdPattern = "^[a-z0-9_.-]+$";
        public static string[] IdReserved => ["search", "edit"];
    }
    public static class Content
    {
        public static string Lore => "lore";
        public static string Social => "social";
        public static string Contacts => "contacts";
        public static string Press => "press";


        public static string[] LinkEditable => [Social, Contacts];
        public static string[] TextEditable => [Lore];
        public static string[] Editable => [.. TextEditable, .. LinkEditable];
    }

    public static class CustomClaim
    {
        public static string Session => "Session";
        public static string Feature => "Feature";
    }

    public static class Cache
    {
        public static string SessionPrefix => "session:";
        public static string ApiKeyPrefix => "api-key:";
    }

    public static class Reference
    {
        public static class AccountStatus
        {
            public static string Enabled => "enabled";
        }
        public static class AccountType
        {
            public static string Automation => "system_automation";
            public static string User => "user";
        }
    }

    public static class Cookie
    {
        public static string Auth => "tld15-main";
        public static string Language => "tld15-language";
        public static string Identity => "tld15-identity";
    }

    public static class ApiKey
    {
        public static string Name => "X-API-KEY";
    }

    public static class License
    {
        public static string Spdx => "AGPL-3.0-only";
    }

    public static class StainlessTasks
    {
        public const int MaxSnapshotsPerAccount = 20;
    }

    public static class Pagination
    {
        public const int PageSize = 25;
        public const int MaxVisiblePages = 7;
    }

    /// <summary> Locales supported by the system </summary>
    /// <remarks>
    /// ISO_639-3. Held rather than built on each read: every page reads the current locale through
    /// <see cref="ToStoredLanguage"/> several times while it renders, and a property that returns a
    /// new dictionary allocates one on every single one of those reads. Handed out read-only so the
    /// one instance cannot be written to from outside, and kept as an insertion-ordered dictionary
    /// because <see cref="LanguageFallback"/> is the first row of it.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> Locales => _locales;

    private static readonly Dictionary<string, string> _locales = new(StringComparer.Ordinal)
    {
        {"en", "English"},
        {"ja", "日本語"}
    };

    /// <summary> The locale a page falls back to when the requested one holds no translation </summary>
    public static string LanguageFallback => Locales.First().Key;

    /// <summary> The requested locale, or the fallback when it names one this site does not store. </summary>
    public static string ToStoredLanguage(string? language)
    {
        return Locales.ContainsKey(language ?? string.Empty) ? language! : LanguageFallback;
    }

    /// <summary> The same locales as OpenGraph writes one: a language and the territory it is read in. </summary>
    /// <remarks>
    /// Keep a row here for every row of <see cref="Locales"/>. The specification asks for
    /// language_TERRITORY, and a consumer handed a bare "en" falls back to its own default - the
    /// preview then comes back in a language the reader never asked for.
    /// </remarks>
    private static readonly Dictionary<string, string> _localesOpenGraph = new(StringComparer.Ordinal)
    {
        {"en", "en_US"},
        {"ja", "ja_JP"}
    };

    /// <summary> One locale as OpenGraph writes it, falling back the way the rest of the site does. </summary>
    public static string ToOpenGraphLocale(string? language)
    {
        return _localesOpenGraph.TryGetValue(language ?? string.Empty, out var value)
            ? value
            : _localesOpenGraph[LanguageFallback];
    }

    public static class Page
    {
        public static string Title => "title";
        public static string Description => "description";

        public static class OpenGraph
        {
            public static string Url => "og:url";
            public static string Image => "og:image";
            public static string ImageAlt => "og:image:alt";
            public static string ImageWidth => "og:image:width";
            public static string ImageHeight => "og:image:height";
            public static string ImageType => "og:image:type";
            public static string Locale => "og:locale";
            public static string LocaleAlternate => "og:locale:alternate";
            public static string Title => "og:title";
            public static string Description => "og:description";
            public static string SiteName => "og:site_name";
            public static string Type => "og:type";
            public static string ArticleAuthor => "article:author";
            public static string ArticlePublished => "article:published_time";
            public static string ArticleModified => "article:modified_time";

            public static class Kind
            {
                public static string Website => "website";
                public static string Article => "article";
            }
        }

        public static class Rss
        {
            public const string MediaType = "application/rss+xml";
        }

        public static class Twitter
        {
            public static string Card => "twitter:card";
            public static string CardLarge => "summary_large_image";
            public static string Site => "twitter:site";
            public static string Creator => "twitter:creator";
        }

        public static class Meta
        {
            public static string Subtitle => "subtitle";
            public static string PageName => "page name";
            public static string Description => "description";
            public static string LastModified => "last-modified";
        }
    }

    public static class Security
    {
        public static string CookieExpiration => $"Security:CookieExpiration";
        public static string CookieMaxAge => $"Security:CookieMaxAge";

        public static string RateLimitWindowSeconds => "Security:RateLimit:WindowSeconds";
        public static string RateLimitGlobalPermits => "Security:RateLimit:GlobalPermits";
        public static string RateLimitApiPermits => "Security:RateLimit:ApiPermits";
    }

    public static class RateLimit
    {
        /// <summary> Policy that the protected API group adds on top of the global limiter </summary>
        public const string ApiPolicy = "api-protected";

        /// <summary> Partition of a caller whose address the server cannot read </summary>
        public const string UnknownPartition = "unknown";

        /// <summary> A permit count of this value or lower turns the limiter off </summary>
        public const int Disabled = 0;
    }

    public static class Settings
    {
        public static string ApplicationHost => "Application:Host";
        public const string SourceUrl = "Application:SourceUrl";
        public const string TwitterSite = "Application:TwitterSite";
        public const string ConnectionString = "PostgreSQL";
        public const string AutomationTimeoutMinutes = "Automation:TimeoutMinutes";
        public static string DateFormat => "yyyy/MM/dd";
    }
}
