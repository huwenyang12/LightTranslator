using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public sealed class ScreenshotTranslationHotkeySettingsPersistence
    : IScreenshotTranslationHotkeyPersistence
{
    private readonly ISettingsService _settingsService;

    public ScreenshotTranslationHotkeySettingsPersistence(
        ISettingsService settingsService
    )
    {
        _settingsService = settingsService;
    }

    public async Task<bool> SaveAsync(
        HotkeyDefinition hotkey,
        CancellationToken cancellationToken = default
    )
    {
        var settings = await _settingsService.LoadAsync(
            cancellationToken
        );

        var updatedSettings = settings with
        {
            ScreenshotTranslationHotkey = hotkey
        };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );

        return true;
    }
}
