using Microsoft.Win32;

namespace LightTranslator.Services.Startup;

public sealed class RegistryStartupRegistrationBackend
    : IStartupRegistrationBackend
{
    private const string RegistryPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName =
        "Bridgo";

    public void Enable(
        string executablePath
    )
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException(
                "Executable path cannot be empty.",
                nameof(executablePath)
            );
        }

        using var key =
            Registry.CurrentUser.CreateSubKey(
                RegistryPath,
                writable: true
            );

        if (key is null)
        {
            throw new InvalidOperationException(
                "Unable to open Windows startup registry key."
            );
        }

        key.SetValue(
            ValueName,
            $"\"{executablePath}\"",
            RegistryValueKind.String
        );
    }

    public void Disable()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                RegistryPath,
                writable: true
            );

        key?.DeleteValue(
            ValueName,
            throwOnMissingValue: false
        );
    }
}