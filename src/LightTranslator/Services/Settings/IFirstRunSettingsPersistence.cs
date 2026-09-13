namespace LightTranslator.Services.Settings;

public interface IFirstRunSettingsPersistence
{
    Task SaveAsync(
        string apiKey,
        CancellationToken cancellationToken = default
    );
}