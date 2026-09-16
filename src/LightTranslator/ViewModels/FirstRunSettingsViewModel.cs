using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;
using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.ViewModels;

public sealed class FirstRunSettingsViewModel
{
    private readonly IFirstRunSettingsPersistence? _persistence;
    private readonly IStartWithWindowsSettingsPersistence? _startupPersistence;
    private readonly IScreenshotLanguageSettingsPersistence? _screenshotLanguagePersistence;
    private readonly IApiKeyValidator? _apiKeyValidator;
    private readonly ITextTranslationHotkeyPersistence? _hotkeyPersistence;
    private readonly ITextTranslationHotkeyChangeService? _hotkeyChangeService;
    private readonly IScreenshotTranslationHotkeyPersistence? _screenshotHotkeyPersistence;
    private readonly IScreenshotTranslationHotkeyChangeService? _screenshotHotkeyChangeService;

    private string _apiKey = string.Empty;
    private HotkeyDefinition? _currentTextTranslationHotkey;
    private HotkeyDefinition? _currentScreenshotTranslationHotkey;

    public bool ApiKeyTestSucceeded { get; set; }
    public string ApiKeyTestMessage { get; private set; } = string.Empty;
    public bool IsTestingApiKey { get; private set; }

    public HotkeyDefinition? TextTranslationHotkey { get; set; }
    public HotkeyDefinition? ScreenshotTranslationHotkey { get; set; }

    public IReadOnlyList<LanguageOption> ScreenshotSourceLanguages =>
        LanguageOption.SourceLanguages;

    public IReadOnlyList<LanguageOption> ScreenshotTargetLanguages =>
        LanguageOption.TargetLanguages;

    public string ScreenshotSourceLanguage { get; set; }
    public string ScreenshotTargetLanguage { get; set; }

    public void SetTextTranslationHotkey(
        HotkeyDefinition hotkey
    )
    {
        TextTranslationHotkey = hotkey;
    }

    public void SetScreenshotTranslationHotkey(
        HotkeyDefinition hotkey
    )
    {
        ScreenshotTranslationHotkey = hotkey;
    }

    public bool SwapScreenshotLanguages()
    {
        if (string.Equals(
                ScreenshotSourceLanguage,
                "auto",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return false;
        }

        var previousSource = ScreenshotSourceLanguage;

        ScreenshotSourceLanguage = ScreenshotTargetLanguage;
        ScreenshotTargetLanguage = previousSource;

        return true;
    }

    public async Task<bool> SaveTextTranslationHotkeyAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (TextTranslationHotkey == _currentTextTranslationHotkey)
        {
            return true;
        }

        if (TextTranslationHotkey is null)
        {
            return false;
        }

        if (_hotkeyChangeService is not null)
        {
            var saved = await _hotkeyChangeService.ApplyAsync(
                _currentTextTranslationHotkey!,
                TextTranslationHotkey,
                cancellationToken
            );

            if (saved)
            {
                _currentTextTranslationHotkey = TextTranslationHotkey;
            }
            else
            {
                TextTranslationHotkey = _currentTextTranslationHotkey;
            }

            return saved;
        }

        if (_hotkeyPersistence is null)
        {
            return false;
        }

        return await _hotkeyPersistence.SaveAsync(
            TextTranslationHotkey,
            cancellationToken
        );
    }

    public async Task<bool> SaveScreenshotTranslationHotkeyAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (ScreenshotTranslationHotkey == _currentScreenshotTranslationHotkey)
        {
            return true;
        }

        if (ScreenshotTranslationHotkey is null)
        {
            return false;
        }

        if (_screenshotHotkeyChangeService is not null)
        {
            var saved = await _screenshotHotkeyChangeService.ApplyAsync(
                _currentScreenshotTranslationHotkey,
                ScreenshotTranslationHotkey,
                cancellationToken
            );

            if (saved)
            {
                _currentScreenshotTranslationHotkey = ScreenshotTranslationHotkey;
            }
            else
            {
                ScreenshotTranslationHotkey = _currentScreenshotTranslationHotkey;
            }

            return saved;
        }

        if (_screenshotHotkeyPersistence is null)
        {
            return false;
        }

        return await _screenshotHotkeyPersistence.SaveAsync(
            ScreenshotTranslationHotkey,
            cancellationToken
        );
    }

    public FirstRunSettingsViewModel(
        IFirstRunSettingsPersistence? persistence = null,
        IStartWithWindowsSettingsPersistence? startupPersistence = null,
        IApiKeyValidator? apiKeyValidator = null,
        ITextTranslationHotkeyPersistence? hotkeyPersistence = null,
        ITextTranslationHotkeyChangeService? hotkeyChangeService = null,
        HotkeyDefinition? currentTextTranslationHotkey = null,
        IScreenshotLanguageSettingsPersistence? screenshotLanguagePersistence = null,
        string? currentScreenshotSourceLanguage = null,
        string? currentScreenshotTargetLanguage = null,
        IScreenshotTranslationHotkeyPersistence? screenshotHotkeyPersistence = null,
        IScreenshotTranslationHotkeyChangeService? screenshotHotkeyChangeService = null,
        HotkeyDefinition? currentScreenshotTranslationHotkey = null
    )
    {
        _persistence = persistence;
        _startupPersistence = startupPersistence;
        _apiKeyValidator = apiKeyValidator;
        _hotkeyPersistence = hotkeyPersistence;
        _hotkeyChangeService = hotkeyChangeService;
        _screenshotLanguagePersistence = screenshotLanguagePersistence;
        _screenshotHotkeyPersistence = screenshotHotkeyPersistence;
        _screenshotHotkeyChangeService = screenshotHotkeyChangeService;

        _currentTextTranslationHotkey = currentTextTranslationHotkey;
        TextTranslationHotkey = _currentTextTranslationHotkey;

        _currentScreenshotTranslationHotkey = currentScreenshotTranslationHotkey;
        ScreenshotTranslationHotkey = _currentScreenshotTranslationHotkey;

        var defaultSettings = AppSettings.CreateDefault();

        ScreenshotSourceLanguage =
            currentScreenshotSourceLanguage
            ?? defaultSettings.ScreenshotSourceLanguage;

        ScreenshotTargetLanguage =
            currentScreenshotTargetLanguage
            ?? defaultSettings.ScreenshotTargetLanguage;
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            var newValue = value ?? string.Empty;

            if (_apiKey == newValue)
            {
                return;
            }

            _apiKey = newValue;
            ApiKeyTestSucceeded = false;
            ApiKeyTestMessage = string.Empty;
        }
    }

    public bool StartWithWindows { get; set; }

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        ApiKeyTestSucceeded;

    public async Task<bool> SaveAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!CanSave || _persistence is null)
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

    public async Task<bool> SaveScreenshotLanguagesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_screenshotLanguagePersistence is null)
        {
            return true;
        }

        try
        {
            await _screenshotLanguagePersistence.SaveAsync(
                ScreenshotSourceLanguage,
                ScreenshotTargetLanguage,
                cancellationToken
            );

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public async Task<bool> TestApiKeyAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (
            _apiKeyValidator is null ||
            string.IsNullOrWhiteSpace(ApiKey)
        )
        {
            return false;
        }

        var apiKeyToValidate = ApiKey;
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

            ApiKeyTestSucceeded = true;
            ApiKeyTestMessage = "连接成功";
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

            ApiKeyTestSucceeded = false;
            ApiKeyTestMessage = "连接失败";
            return false;
        }
        finally
        {
            IsTestingApiKey = false;
        }
    }
}
