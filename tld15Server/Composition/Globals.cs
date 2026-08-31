// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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

    public static class ProjectType
    {
        public static string Project => "project";
        public static string Article => "article";
    }
    public static class Content
    {
        public static string Lore => "lore";
        public static string Social => "social";
        public static string Contacts => "contacts";
        public static string Press => "press";

        public static string[] LinkEditable => [Social, Contacts];
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
    /// <remarks> ISO_639-3 </remarks>
    public static Dictionary<string, string> Locales => new()
    {
        {"en", "English"},
        {"ja", "日本語"}
    };

    /// <summary> The locale a page falls back to when the requested one holds no translation </summary>
    public static string LanguageFallback => Locales.First().Key;

    public static class Page
    {
        public static string Title => "title";
        public static string Description => "description";

        public static class OpenGraph
        {
            public static string Url => "og:url";
            public static string Image => "og:image";
            public static string Locale => "og:locale";
            public static string Title => "og:title";
            public static string Description => "og:description";
            public static string SiteName => "og:site_name";
            public static string Type => "og:type";
            public static string ArticleAuthor => "article:author";
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
        public const string ConnectionString = "PostgreSQL";
        public const string AutomationTimeoutMinutes = "Automation:TimeoutMinutes";
        public static string DateFormat => "yyyy/MM/dd";
    }
}
