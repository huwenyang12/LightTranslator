using System.Windows.Input;
using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class HotkeyCaptureParserTests
{
    [Fact]
    public void TryCapture_WithAltT_CreatesHotkey()
    {
        var captured =
            HotkeyCaptureParser.TryCapture(
                Key.T,
                ModifierKeys.Alt,
                out var hotkey
            );

        Assert.True(
            captured
        );

        Assert.Equal(
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            ),
            hotkey
        );
    }

    [Fact]
    public void TryCapture_WithAltSystemKey_UsesSystemKey()
    {
        var captured =
            HotkeyCaptureParser.TryCapture(
                Key.System,
                Key.T,
                ModifierKeys.Alt,
                out var hotkey
            );

        Assert.True(
            captured
        );

        Assert.Equal(
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            ),
            hotkey
        );
    }
}