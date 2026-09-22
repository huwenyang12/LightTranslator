using System.Reflection;

namespace LightTranslator.Services.Windows;

internal static class ApplicationVersionDisplay
{
    public static string FromAssembly(
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(
            assembly
        );

        return
            Format(
                assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion,
                assembly.GetName().Version
            );
    }

    public static string Format(
        string? informationalVersion,
        Version? assemblyVersion
    )
    {
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var normalized =
                informationalVersion
                    .Split(
                        '+',
                        2
                    )[0]
                    .Trim();

            if (normalized.Length > 0)
            {
                return normalized;
            }
        }

        if (assemblyVersion is null)
        {
            return "0.0.0";
        }

        return
            $"{assemblyVersion.Major}." +
            $"{assemblyVersion.Minor}." +
            $"{Math.Max(0, assemblyVersion.Build)}";
    }
}
