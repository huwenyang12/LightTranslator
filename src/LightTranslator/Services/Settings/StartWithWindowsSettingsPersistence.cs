using LightTranslator.Services.Startup;

namespace LightTranslator.Services.Settings;

public sealed class StartWithWindowsSettingsPersistence
    : IStartWithWindowsSettingsPersistence
{
    private readonly StartupService _startupService;

    private readonly ISettingsService _settingsService;

    public StartWithWindowsSettingsPersistence(
        StartupService startupService,
        ISettingsService settingsService
    )
    {
        _startupService =
            startupService;

        _settingsService =
            settingsService;
    }

    public async Task SaveAsync(
        bool enabled,
        CancellationToken cancellationToken = default
    )
    {
        var settings =
            await _settingsService.LoadAsync(
                cancellationToken
            );

        _startupService.SetEnabled(
            enabled
        );

        var updatedSettings =
            settings with
            {
                StartWithWindows =
                    enabled
            };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );
    }
}