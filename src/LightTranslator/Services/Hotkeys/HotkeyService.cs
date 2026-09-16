using LightTranslator.Models;

namespace LightTranslator.Services.Hotkeys;

public sealed class HotkeyService
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWindows = 0x0008;

    private readonly IHotkeyBackend _backend;
    private bool _requestsSuspended;

    public const int TextTranslationHotkeyId = 1;
    public const int ScreenshotTranslationHotkeyId = 2;

    public event Action? TextTranslationRequested;
    public event Action? ScreenshotTranslationRequested;

    public HotkeyService(
        IHotkeyBackend backend
    )
    {
        _backend = backend;
        _backend.HotkeyPressed += OnHotkeyPressed;
    }

    public void SuspendRequests()
    {
        _requestsSuspended = true;
    }

    public void ResumeRequests()
    {
        _requestsSuspended = false;
    }

    public bool ReplaceTextTranslation(
        HotkeyDefinition? oldHotkey,
        HotkeyDefinition? newHotkey
    )
    {
        return ReplaceHotkey(
            TextTranslationHotkeyId,
            oldHotkey,
            newHotkey
        );
    }

    public bool ReplaceScreenshotTranslation(
        HotkeyDefinition? oldHotkey,
        HotkeyDefinition? newHotkey
    )
    {
        return ReplaceHotkey(
            ScreenshotTranslationHotkeyId,
            oldHotkey,
            newHotkey
        );
    }

    public bool RegisterTextTranslation(
        HotkeyDefinition? hotkey
    )
    {
        return RegisterHotkey(
            TextTranslationHotkeyId,
            hotkey
        );
    }

    public bool RegisterScreenshotTranslation(
        HotkeyDefinition? hotkey
    )
    {
        return RegisterHotkey(
            ScreenshotTranslationHotkeyId,
            hotkey
        );
    }

    private bool RegisterHotkey(
        int id,
        HotkeyDefinition? hotkey
    )
    {
        if (hotkey is null)
        {
            return true;
        }

        var modifiers = GetModifiers(hotkey);
        var virtualKey = GetVirtualKey(hotkey.Key);

        return _backend.Register(
            id,
            modifiers,
            virtualKey
        );
    }

    private bool ReplaceHotkey(
        int id,
        HotkeyDefinition? oldHotkey,
        HotkeyDefinition? newHotkey
    )
    {
        _backend.Unregister(id);

        if (newHotkey is null)
        {
            return true;
        }

        var registered = RegisterHotkey(
            id,
            newHotkey
        );

        if (registered)
        {
            return true;
        }

        if (oldHotkey is not null)
        {
            RegisterHotkey(
                id,
                oldHotkey
            );
        }

        return false;
    }

    private void OnHotkeyPressed(
        int id
    )
    {
        if (_requestsSuspended)
        {
            return;
        }

        if (id == TextTranslationHotkeyId)
        {
            TextTranslationRequested?.Invoke();
            return;
        }

        if (id == ScreenshotTranslationHotkeyId)
        {
            ScreenshotTranslationRequested?.Invoke();
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

        return char.ToUpperInvariant(key[0]);
    }
}
