namespace LightTranslator.Services.Settings;

public sealed class ScreenshotLanguageSettingsPersistence
    : IScreenshotLanguageSettingsPersistence
{
    private static readonly HashSet<string> SupportedSourceLanguages =
        new(
            new[]
            {
                "auto",
                "zh",
                "en",
                "ja"
            },
            StringComparer.Ordinal
        );

    private static readonly HashSet<string> SupportedTargetLanguages =
        new(
            new[]
            {
                "zh",
                "en",
                "ja"
            },
            StringComparer.Ordinal
        );

    private readonly ISettingsService _settingsService;

    public ScreenshotLanguageSettingsPersistence(
        ISettingsService settingsService
    )
    {
        _settingsService =
            settingsService ??
            throw new ArgumentNullException(
                nameof(settingsService)
            );
    }

    public async Task SaveAsync(
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        ValidateLanguages(
            sourceLanguage,
            targetLanguage
        );

        var settings =
            await _settingsService.LoadAsync(
                cancellationToken
            );

        var updatedSettings =
            settings with
            {
                ScreenshotSourceLanguage =
                    sourceLanguage,

                ScreenshotTargetLanguage =
                    targetLanguage
            };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );
    }

    private static void ValidateLanguages(
        string sourceLanguage,
        string targetLanguage
    )
    {
        if (!SupportedSourceLanguages.Contains(sourceLanguage))
        {
            throw new ArgumentException(
                "Unsupported screenshot source language.",
                nameof(sourceLanguage)
            );
        }

        if (!SupportedTargetLanguages.Contains(targetLanguage))
        {
            throw new ArgumentException(
                "Unsupported screenshot target language.",
                nameof(targetLanguage)
            );
        }

        if (
            !string.Equals(
                sourceLanguage,
                "auto",
                StringComparison.Ordinal
            ) &&
            string.Equals(
                sourceLanguage,
                targetLanguage,
                StringComparison.Ordinal
            )
        )
        {
            throw new ArgumentException(
                "Screenshot source and target languages must differ when the source language is explicit.",
                nameof(targetLanguage)
            );
        }
    }
}
