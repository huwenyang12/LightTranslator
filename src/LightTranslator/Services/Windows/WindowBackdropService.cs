using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LightTranslator.Services.Windows;

public enum WindowBackdropKind
{
    Mica,
    TransientAcrylic
}

public static class WindowBackdropService
{
    private const int DwmWindowAttributeCornerPreference =
        33;

    private const int DwmWindowAttributeSystemBackdropType =
        38;

    private const int DwmWindowCornerPreferenceRound =
        2;

    private const int DwmSystemBackdropMainWindow =
        2;

    private const int DwmSystemBackdropTransientWindow =
        3;

    public static bool TryApply(
        Window window,
        WindowBackdropKind kind
    )
    {
        ArgumentNullException.ThrowIfNull(
            window
        );

        if (!IsSupported(
                Environment.OSVersion.Version
            ))
        {
            return false;
        }

        var windowHandle =
            new WindowInteropHelper(
                window
            ).Handle;

        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        var cornerPreference =
            DwmWindowCornerPreferenceRound;

        var backdropType =
            kind == WindowBackdropKind.Mica
                ? DwmSystemBackdropMainWindow
                : DwmSystemBackdropTransientWindow;

        try
        {
            _ =
                DwmSetWindowAttribute(
                    windowHandle,
                    DwmWindowAttributeCornerPreference,
                    ref cornerPreference,
                    Marshal.SizeOf<int>()
                );

            var result =
                DwmSetWindowAttribute(
                    windowHandle,
                    DwmWindowAttributeSystemBackdropType,
                    ref backdropType,
                    Marshal.SizeOf<int>()
                );

            return result >= 0;
        }
        catch (
            Exception exception
        ) when (
            exception is DllNotFoundException or
            EntryPointNotFoundException
        )
        {
            return false;
        }
    }

    internal static bool IsSupported(
        Version operatingSystemVersion
    )
    {
        ArgumentNullException.ThrowIfNull(
            operatingSystemVersion
        );

        return operatingSystemVersion >=
            new Version(
                10,
                0,
                22621
            );
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize
    );
}
