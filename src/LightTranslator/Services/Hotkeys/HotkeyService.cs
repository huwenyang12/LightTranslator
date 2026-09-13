using LightTranslator.Models;

namespace LightTranslator.Services.Hotkeys;

public sealed class HotkeyService
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWindows = 0x0008;

    private readonly IHotkeyBackend _backend;

    public const int TextTranslationHotkeyId = 1;

    public event Action? TextTranslationRequested;

    public HotkeyService(
        IHotkeyBackend backend
    )
    {
        _backend = backend;

        _backend.HotkeyPressed +=
            OnHotkeyPressed;
    }

    public bool RegisterTextTranslation(
        HotkeyDefinition hotkey
    )
    {
        var modifiers =
            GetModifiers(hotkey);

        var virtualKey =
            GetVirtualKey(hotkey.Key);

        return _backend.Register(
            TextTranslationHotkeyId,
            modifiers,
            virtualKey
        );
    }

    private void OnHotkeyPressed(
        int id
    )
    {
        if (
            id ==
            TextTranslationHotkeyId
        )
        {
            TextTranslationRequested?.Invoke();
        }
    }

    private static uint GetModifiers(
        HotkeyDefinition hotkey
    )
    {
        uint modifiers = 0;

        if (hotkey.Alt)
        {
            modifiers |= ModAlt;
        }

        if (hotkey.Control)
        {
            modifiers |= ModControl;
        }

        if (hotkey.Shift)
        {
            modifiers |= ModShift;
        }

        if (hotkey.Windows)
        {
            modifiers |= ModWindows;
        }

        return modifiers;
    }

    private static uint GetVirtualKey(
        string key
    )
    {
        if (
            string.IsNullOrWhiteSpace(key) ||
            key.Length != 1
        )
        {
            throw new ArgumentException(
                "Hotkey key must be a single character.",
                nameof(key)
            );
        }

        return char.ToUpperInvariant(
            key[0]
        );
    }
}