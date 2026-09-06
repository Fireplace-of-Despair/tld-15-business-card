// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

namespace StainlessCore.Features;

/// <summary>
/// One vertical slice of the application. The startup reflects over every feature and registers one
/// authorization policy for each identifier.
/// </summary>
public interface IFeature
{
    /// <summary> Get the identifier of the feature, for example <c>identity.post</c>. </summary>
    public static abstract string FeatureId { get; }
}
