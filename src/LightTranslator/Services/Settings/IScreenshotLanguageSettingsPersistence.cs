namespace LightTranslator.Services.Settings;

public interface IScreenshotLanguageSettingsPersistence
{
    Task SaveAsync(
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    );
}
