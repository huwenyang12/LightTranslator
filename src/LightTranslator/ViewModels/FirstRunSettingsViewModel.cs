using LightTranslator.Services.Settings;

namespace LightTranslator.ViewModels;

public sealed class FirstRunSettingsViewModel
{
    private readonly IFirstRunSettingsPersistence? _persistence;

    private string _apiKey = string.Empty;

    public FirstRunSettingsViewModel()
    {
    }

    public FirstRunSettingsViewModel(
        IFirstRunSettingsPersistence persistence
    )
    {
        _persistence =
            persistence;
    }

    public string ApiKey
    {
        get => _apiKey;

        set
        {
            _apiKey =
                value ?? string.Empty;
        }
    }

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(
            ApiKey
        );

    public async Task<bool> SaveAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (
            !CanSave ||
            _persistence is null
        )
        {
            return false;
        }

        await _persistence.SaveAsync(
            ApiKey,
            cancellationToken
        );

        return true;
    }
}