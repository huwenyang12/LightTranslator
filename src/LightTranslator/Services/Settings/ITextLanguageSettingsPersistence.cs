namespace LightTranslator.Services.Settings;

public interface ITextLanguageSettingsPersistence
{
    Task SaveAsync(
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    );
}