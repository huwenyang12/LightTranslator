using LightTranslator.Infrastructure.Security;

namespace LightTranslator.Services.Settings;

public sealed class FirstRunSettingsPersistence
    : IFirstRunSettingsPersistence
{
    private const string ApiKeyName =
        "deepseek-api-key";

    private readonly ISecretStorage _secretStorage;

    private readonly ISettingsService _settingsService;

    public FirstRunSettingsPersistence(
        ISecretStorage secretStorage,
        ISettingsService settingsService
    )
    {
        _secretStorage =
            secretStorage;

        _settingsService =
            settingsService;
    }

    public async Task SaveAsync(
        string apiKey,
        CancellationToken cancellationToken = default
    )
    {
        var settings =
            await _settingsService.LoadAsync(
                cancellationToken
            );

        _secretStorage.Save(
            ApiKeyName,
            apiKey
        );

        var updatedSettings =
            settings with
            {
                FirstRunCompleted = true
            };

        await _settingsService.SaveAsync(
            updatedSettings,
            cancellationToken
        );
    }
}