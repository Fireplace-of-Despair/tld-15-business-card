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
