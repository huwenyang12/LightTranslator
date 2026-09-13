namespace LightTranslator.Services.Settings;

public interface IStartWithWindowsSettingsPersistence
{
    Task SaveAsync(
        bool enabled,
        CancellationToken cancellationToken = default
    );
}