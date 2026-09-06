// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System;
using System.Collections.Generic;
using System.Linq;
using StainlessCore.Composition;

namespace StainlessCore.Common.Helpers;

/// <summary>
/// Helper for translated entities in an <c>IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt;</c>
/// format. The key holds the language. The value holds the text.
/// </summary>
public static class TranslationHelper
{
    /// <summary> Get translation</summary>
    /// <param name="translations">Translation, key: language, value: value</param>
    /// <param name="language">Language to lookup in the collection</param>
    /// <returns>Translation, fallback or "ERROR"</returns>
    public static string GetName(this IEnumerable<KeyValuePair<string, string>> translations, string language)
    {
        var match = translations
            .FirstOrDefault(x => string.Equals(x.Key, language, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (!string.IsNullOrWhiteSpace(match)) { return match; }

        var fallback = translations.FirstOrDefault().Value;

        return !string.IsNullOrWhiteSpace(fallback)
            ? fallback
            : Globals.Error.Text;
    }
}
