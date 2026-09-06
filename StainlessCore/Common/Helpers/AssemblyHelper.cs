// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using System.Reflection;

namespace StainlessCore.Common.Helpers;

/// <summary> <see cref="Assembly"/> helper </summary>
public static class AssemblyHelper
{
    /// <summary> Get the version of an assembly as text. </summary>
    /// <param name="assembly">The assembly to read.</param>
    /// <returns>
    /// The informational version. When that attribute is empty, the assembly version. When both are
    /// empty, an empty string.
    /// </returns>
    public static string GetVersion(this Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }
}
