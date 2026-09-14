using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public sealed class TextTranslationHotkeySettingsPersistence
    : ITextTranslationHotkeyPersistence
{
    private readonly ISettingsService _settingsService;

    public TextTranslationHotkeySettingsPersistence(
        ISettingsService settingsService
    )
    {
        _settingsService =
            settingsService;
    }

    public async Task<bool> SaveAsync(
        HotkeyDefinition hotkey,
        CancellationToken cancellationToken = default
    )
    {
        var settings =
            await _settingsService.LoadAsync(
                cancellationToken
            );

        var updatedSettings =
            settings with
            {
                TextTranslationHotkey =
                    hotkey
            };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );

        return true;
    }
}