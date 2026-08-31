// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

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
