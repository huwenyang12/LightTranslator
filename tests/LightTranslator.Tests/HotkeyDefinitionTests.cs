using LightTranslator.Models;

namespace LightTranslator.Tests;

public class HotkeyDefinitionTests
{
    [Fact]
    public void TryCreate_WithModifierAndLetter_CreatesHotkey()
    {
        var created =
            HotkeyDefinition.TryCreate(
                "T",
                alt: true,
                control: false,
                shift: false,
                windows: false,
                out var hotkey
            );

        Assert.True(
            created
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
    public void TryCreate_WithoutModifier_ReturnsFalse()
    {
        var created =
            HotkeyDefinition.TryCreate(
                "T",
                alt: false,
                control: false,
                shift: false,
                windows: false,
                out var hotkey
            );

        Assert.False(
            created
        );

        Assert.Null(
            hotkey
        );
    }

    [Fact]
    public void TryCreate_WithoutMainKey_ReturnsFalse()
    {
        var created =
            HotkeyDefinition.TryCreate(
                string.Empty,
                alt: true,
                control: false,
                shift: false,
                windows: false,
                out var hotkey
            );

        Assert.False(
            created
        );

        Assert.Null(
            hotkey
        );
    }

    [Fact]
    public void TryCreate_WithMultipleCharacters_ReturnsFalse()
    {
        var created =
            HotkeyDefinition.TryCreate(
                "TT",
                alt: true,
                control: false,
                shift: false,
                windows: false,
                out var hotkey
            );

        Assert.False(
            created
        );

        Assert.Null(
            hotkey
        );
    }

    [Fact]
    public void TryCreate_WithLowercaseLetter_NormalizesToUppercase()
    {
        var created =
            HotkeyDefinition.TryCreate(
                "t",
                alt: true,
                control: false,
                shift: false,
                windows: false,
                out var hotkey
            );

        Assert.True(
            created
        );

        Assert.NotNull(
            hotkey
        );

        Assert.Equal(
            "T",
            hotkey.Key
        );
    }
}