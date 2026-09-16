using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Services.Hotkeys;

public sealed class ScreenshotTranslationHotkeyChangeService
    : IScreenshotTranslationHotkeyChangeService
{
    private readonly HotkeyService _hotkeyService;
    private readonly IScreenshotTranslationHotkeyPersistence _persistence;

    public ScreenshotTranslationHotkeyChangeService(
        HotkeyService hotkeyService,
        IScreenshotTranslationHotkeyPersistence persistence
    )
    {
        _hotkeyService = hotkeyService;
        _persistence = persistence;
    }

    public async Task<bool> ApplyAsync(
        HotkeyDefinition? oldHotkey,
        HotkeyDefinition newHotkey,
        CancellationToken cancellationToken = default
    )
    {
        var replaced = _hotkeyService.ReplaceScreenshotTranslation(
            oldHotkey,
            newHotkey
        );

        if (!replaced)
        {
            return false;
        }

        var persisted = await _persistence.SaveAsync(
            newHotkey,
            cancellationToken
        );

        if (persisted)
        {
            return true;
        }

        _hotkeyService.ReplaceScreenshotTranslation(
            newHotkey,
            oldHotkey
        );

        return false;
    }
}
