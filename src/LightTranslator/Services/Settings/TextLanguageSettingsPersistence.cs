namespace LightTranslator.Services.Settings;

public sealed class TextLanguageSettingsPersistence
    : ITextLanguageSettingsPersistence
{
    private readonly ISettingsService _settingsService;

    public TextLanguageSettingsPersistence(
        ISettingsService settingsService
    )
    {
        _settingsService =
            settingsService;
    }

    public async Task SaveAsync(
        string sourceLanguage,
        string targetLanguage,
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
                TextSourceLanguage =
                    sourceLanguage,

                TextTargetLanguage =
                    targetLanguage
            };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );
    }
}