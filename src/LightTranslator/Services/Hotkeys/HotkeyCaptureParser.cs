using System.Windows.Input;

using LightTranslator.Models;

namespace LightTranslator.Services.Hotkeys;

public static class HotkeyCaptureParser
{

    public static bool TryCapture(
        Key key,
        Key systemKey,
        ModifierKeys modifiers,
        out HotkeyDefinition? hotkey
    )
    {
        var effectiveKey =
            key == Key.System
                ? systemKey
                : key;

        return TryCapture(
            effectiveKey,
            modifiers,
            out hotkey
        );
    }
    public static bool TryCapture(
        Key key,
        ModifierKeys modifiers,
        out HotkeyDefinition? hotkey
    )
    {
        return HotkeyDefinition.TryCreate(
            key.ToString(),
            alt:
                modifiers.HasFlag(
                    ModifierKeys.Alt
                ),
            control:
                modifiers.HasFlag(
                    ModifierKeys.Control
                ),
            shift:
                modifiers.HasFlag(
                    ModifierKeys.Shift
                ),
            windows:
                modifiers.HasFlag(
                    ModifierKeys.Windows
                ),
            out hotkey
        );
    }
}