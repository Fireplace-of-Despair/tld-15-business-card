// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

namespace StainlessCore.Models;

/// <summary> One translation row of a multi-language entity. </summary>
public interface ITranslation
{
    /// <summary> Get or set the language of the row. </summary>
    public string LanguageId { get; set; }
}
