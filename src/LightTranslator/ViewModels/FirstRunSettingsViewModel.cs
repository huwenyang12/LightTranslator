using LightTranslator.Services.Settings;

namespace LightTranslator.ViewModels;

public sealed class FirstRunSettingsViewModel
{
    private readonly IFirstRunSettingsPersistence? _persistence;

    private readonly IStartWithWindowsSettingsPersistence?
        _startupPersistence;

    private string _apiKey = string.Empty;

    public FirstRunSettingsViewModel(
        IFirstRunSettingsPersistence? persistence = null,
        IStartWithWindowsSettingsPersistence? startupPersistence = null
    )
    {
        _persistence =
            persistence;

        _startupPersistence =
            startupPersistence;
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

    public bool StartWithWindows { get; set; }

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

    public async Task<bool> SaveStartWithWindowsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_startupPersistence is null)
        {
            return false;
        }

        await _startupPersistence.SaveAsync(
            StartWithWindows,
            cancellationToken
        );

        return true;
    }
}