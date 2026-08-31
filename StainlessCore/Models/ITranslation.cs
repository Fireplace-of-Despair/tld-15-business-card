// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

namespace StainlessCore.Models;

/// <summary> One translation row of a multi-language entity. </summary>
public interface ITranslation
{
    /// <summary> Get or set the language of the row. </summary>
    public string LanguageId { get; set; }
}
