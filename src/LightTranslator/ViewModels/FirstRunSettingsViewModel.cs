using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;
namespace LightTranslator.ViewModels;

public sealed class FirstRunSettingsViewModel
{
    private readonly IFirstRunSettingsPersistence? _persistence;

    private readonly IStartWithWindowsSettingsPersistence?
        _startupPersistence;

    private string _apiKey = string.Empty;
    public bool ApiKeyTestSucceeded { get; set; }
    public string ApiKeyTestMessage { get; private set; } =
    string.Empty;
    private readonly IApiKeyValidator? _apiKeyValidator;
    public bool IsTestingApiKey { get; private set; }
    public FirstRunSettingsViewModel(
        IFirstRunSettingsPersistence? persistence = null,
        IStartWithWindowsSettingsPersistence? startupPersistence = null,
        IApiKeyValidator? apiKeyValidator = null
    )
    {
        _persistence =
            persistence;

        _startupPersistence =
            startupPersistence;

        _apiKeyValidator =
            apiKeyValidator;
    }

    public string ApiKey
    {
        get => _apiKey;

        set
        {
            var newValue =
                value ?? string.Empty;

            if (_apiKey == newValue)
            {
                return;
            }

            _apiKey =
                newValue;

            ApiKeyTestSucceeded =
                false;

            ApiKeyTestMessage =
                string.Empty;
        }
    }

    public bool StartWithWindows { get; set; }

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(
            ApiKey
        ) &&
        ApiKeyTestSucceeded;

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

    public async Task<bool> TestApiKeyAsync(
        CancellationToken cancellationToken = default)
    {
        if (_apiKeyValidator is null ||
            string.IsNullOrWhiteSpace(ApiKey))
        {
            return false;
        }

        var apiKeyToValidate =
            ApiKey;

        IsTestingApiKey = true;

        try
        {
            await _apiKeyValidator.ValidateAsync(
                apiKeyToValidate,
                cancellationToken
            );

            if (!string.Equals(
                    ApiKey,
                    apiKeyToValidate,
                    StringComparison.Ordinal
                ))
            {
                return false;
            }

            ApiKeyTestSucceeded =
                true;

            ApiKeyTestMessage =
                "连接成功";

            return true;
        }
        catch
        {
            if (!string.Equals(
                    ApiKey,
                    apiKeyToValidate,
                    StringComparison.Ordinal
                ))
            {
                return false;
            }

            ApiKeyTestSucceeded =
                false;

            ApiKeyTestMessage =
                "连接失败";

            return false;
        }
        finally
        {
            IsTestingApiKey =
                false;
        }
    }
}