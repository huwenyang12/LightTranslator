using System.Net.Http;
using LightTranslator.Controllers;
using LightTranslator.Infrastructure.Security;
using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.Services.Logging;
using LightTranslator.Services.Ocr;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Startup;
using LightTranslator.Services.Translation;
using LightTranslator.Services.Tray;
using LightTranslator.Services.Windows;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator;

public partial class App
    : System.Windows.Application,
      IAppStartupView,
      IScreenshotTranslationView
{
    private HttpClient? _httpClient;
    private IApiKeyValidator? _apiKeyValidator;
    private ISecretStorage? _secretStorage;

    private IFirstRunSettingsPersistence? _firstRunSettingsPersistence;
    private IStartWithWindowsSettingsPersistence? _startWithWindowsSettingsPersistence;
    private IScreenshotLanguageSettingsPersistence? _screenshotLanguageSettingsPersistence;
    private IScreenshotTranslationHotkeyPersistence? _screenshotTranslationHotkeyPersistence;
    private bool _startWithWindows;

    private HotkeyMessageWindow? _hotkeyMessageWindow;
    private Win32HotkeyBackend? _hotkeyBackend;
    private HotkeyService? _hotkeyService;
    private WindowManager? _windowManager;
    private TrayService? _trayService;
    private AppController? _appController;

    private ISettingsService? _settingsService;

    private ITextTranslationHotkeyPersistence?
        _textTranslationHotkeyPersistence;

    private HotkeyDefinition?
        _startupTextTranslationHotkey;

    private HotkeyDefinition?
        _startupScreenshotTranslationHotkey;

    private string _startupScreenshotSourceLanguage =
        AppSettings.CreateDefault()
            .ScreenshotSourceLanguage;

    private string _startupScreenshotTargetLanguage =
        AppSettings.CreateDefault()
            .ScreenshotTargetLanguage;

    private ScreenshotTranslationCoordinator?
        _screenshotTranslationCoordinator;

    protected override async void OnStartup(
        System.Windows.StartupEventArgs e
    )
    {
        base.OnStartup(e);

        var settingsService =
            new SettingsService();

        _settingsService =
            settingsService;

        _textTranslationHotkeyPersistence =
            new TextTranslationHotkeySettingsPersistence(
                settingsService
            );

        _screenshotTranslationHotkeyPersistence =
            new ScreenshotTranslationHotkeySettingsPersistence(
                settingsService
            );

        _screenshotLanguageSettingsPersistence =
            new ScreenshotLanguageSettingsPersistence(
                settingsService
            );

        _secretStorage =
            new DpapiSecretStorage();

        _firstRunSettingsPersistence =
            new FirstRunSettingsPersistence(
                _secretStorage,
                settingsService
            );

        var settings =
            await settingsService.LoadAsync();

        _startupTextTranslationHotkey =
            settings.TextTranslationHotkey;

        _startupScreenshotTranslationHotkey =
            settings.ScreenshotTranslationHotkey;

        _startupScreenshotSourceLanguage =
            settings.ScreenshotSourceLanguage;

        _startupScreenshotTargetLanguage =
            settings.ScreenshotTargetLanguage;

        var startupBackend =
            new RegistryStartupRegistrationBackend();

        var startupService =
            new StartupService(
                startupBackend,
                () =>
                    Environment.ProcessPath
                    ?? throw new InvalidOperationException(
                        "Unable to determine application executable path."
                    )
            );

        _startWithWindowsSettingsPersistence =
            new StartWithWindowsSettingsPersistence(
                startupService,
                settingsService
            );

        _startWithWindows =
            settings.StartWithWindows;

        startupService.SetEnabled(
            settings.StartWithWindows
        );

        _httpClient =
            new HttpClient();

        _apiKeyValidator =
            new DeepSeekApiKeyValidator(
                _httpClient
            );

        _appController =
            new AppController(
                this,
                this
            );

        var shouldContinue =
            _appController.Start(
                settings
            );

        if (!shouldContinue)
        {
            Shutdown();
            return;
        }

        settings =
            await settingsService.LoadAsync();

        _startupTextTranslationHotkey =
            settings.TextTranslationHotkey;

        _startupScreenshotTranslationHotkey =
            settings.ScreenshotTranslationHotkey;

        _startupScreenshotSourceLanguage =
            settings.ScreenshotSourceLanguage;

        _startupScreenshotTargetLanguage =
            settings.ScreenshotTargetLanguage;

        _startWithWindows =
            settings.StartWithWindows;

        var deepSeekTranslationService =
            new DeepSeekTranslationService(
                _httpClient,
                () =>
                    _secretStorage.Load(
                        "deepseek-api-key"
                    )
            );

        var logSink =
            new FileLogSink(
                LogFilePathProvider.GetDefaultPath()
            );

        var appLogger =
            new AppLogger(
                logSink
            );

        var translationService =
            new LoggingTranslationService(
                deepSeekTranslationService,
                appLogger
            );

        var displayCaptureService =
            new DisplayCaptureService();

        var screenshotCaptureView =
            new ScreenshotCaptureView();

        var ocrModelProvider =
            new OcrModelProvider();

        var ocrService =
            new PaddleOcrService(
                ocrModelProvider
            );

        var screenshotTextTranslator =
            new DeepSeekScreenshotTextTranslator(
                _httpClient,
                () =>
                    _secretStorage.Load(
                        "deepseek-api-key"
                    )
            );

        var screenshotResultViewFactory =
            new ScreenshotResultViewFactory();

        _screenshotTranslationCoordinator =
            new ScreenshotTranslationCoordinator(
                displayCaptureService,
                screenshotCaptureView,
                ocrService,
                screenshotTextTranslator,
                screenshotResultViewFactory,
                settingsService,
                appLogger
            );

        var textLanguagePersistence =
            new TextLanguageSettingsPersistence(
                settingsService
            );

        var currentSettings =
            settings;

        _windowManager =
            new WindowManager(
                () =>
                {
                    var viewModel =
                        new TranslateViewModel(
                            translationService,
                            initialSourceLanguage:
                                currentSettings.TextSourceLanguage,
                            initialTargetLanguage:
                                currentSettings.TextTargetLanguage
                        );

                    var window =
                        new TranslateWindow(
                            viewModel,
                            textLanguagePersistence
                        );

                    window.Closed +=
                        (_, _) =>
                        {
                            currentSettings =
                                currentSettings with
                                {
                                    TextSourceLanguage =
                                        viewModel.SourceLanguage,

                                    TextTargetLanguage =
                                        viewModel.TargetLanguage
                                };
                        };

                    return window;
                }
            );

        var router =
            new HotkeyMessageRouter();

        _hotkeyMessageWindow =
            new HotkeyMessageWindow(
                router
            );

        var nativeApi =
            new User32HotkeyNativeApi();

        _hotkeyBackend =
            new Win32HotkeyBackend(
                _hotkeyMessageWindow.Handle,
                nativeApi,
                router
            );

        _hotkeyService =
            new HotkeyService(
                _hotkeyBackend
            );

        _hotkeyService.TextTranslationRequested +=
            _windowManager.ToggleTranslateWindow;

        _hotkeyService.ScreenshotTranslationRequested +=
            _appController.OpenScreenshotTranslation;

        var registered =
            _hotkeyService.RegisterTextTranslation(
                settings.TextTranslationHotkey
            );

        if (!registered)
        {
            System.Windows.MessageBox.Show(
                "全局快捷键注册失败，可能已被其他程序占用。",
                "语桥"
            );

            Shutdown();
            return;
        }

        var screenshotRegistered =
            _hotkeyService.RegisterScreenshotTranslation(
                settings.ScreenshotTranslationHotkey
            );

        if (!screenshotRegistered)
        {
            System.Windows.MessageBox.Show(
                "截图翻译快捷键注册失败，可能已被其他程序占用。",
                "语桥"
            );

            Shutdown();
            return;
        }

        var trayBackend =
            new NotifyIconTrayBackend();

        _trayService =
            new TrayService(
                trayBackend
            );

        _trayService.TextTranslationRequested +=
            OnTrayTextTranslationRequested;

        _trayService.SettingsRequested +=
            OnTraySettingsRequested;

        _trayService.ExitRequested +=
            OnTrayExitRequested;

        _trayService.ScreenshotTranslationRequested +=
            OnTrayScreenshotTranslationRequested;

        _trayService.Start();
    }

    private void OnTrayTextTranslationRequested()
    {
        _windowManager?.ToggleTranslateWindow();
    }

    private void OnTrayScreenshotTranslationRequested()
    {
        _appController?.OpenScreenshotTranslation();
    }

    private void OnTraySettingsRequested()
    {
        _appController?.OpenSettings();
    }

    private void OnTrayExitRequested()
    {
        Shutdown();
    }

    public bool ShowFirstRunSettings()
    {
        if (
            _firstRunSettingsPersistence is null ||
            _startWithWindowsSettingsPersistence is null ||
            _apiKeyValidator is null ||
            _textTranslationHotkeyPersistence is null ||
            _screenshotTranslationHotkeyPersistence is null ||
            _screenshotLanguageSettingsPersistence is null
        )
        {
            return false;
        }

        var viewModel =
            new FirstRunSettingsViewModel(
                _firstRunSettingsPersistence,
                _startWithWindowsSettingsPersistence,
                _apiKeyValidator,
                hotkeyPersistence:
                    _textTranslationHotkeyPersistence,
                currentTextTranslationHotkey:
                    _startupTextTranslationHotkey,
                screenshotLanguagePersistence:
                    _screenshotLanguageSettingsPersistence,
                currentScreenshotSourceLanguage:
                    _startupScreenshotSourceLanguage,
                currentScreenshotTargetLanguage:
                    _startupScreenshotTargetLanguage,
                screenshotHotkeyPersistence:
                    _screenshotTranslationHotkeyPersistence,
                currentScreenshotTranslationHotkey:
                    _startupScreenshotTranslationHotkey
            )
            {
                StartWithWindows =
                    _startWithWindows
            };

        var window =
            new FirstRunSettingsWindow(
                viewModel,
                isFirstRun: true,
                isDialogMode: true
            );

        window.Closed +=
            (_, _) =>
            {
                _startWithWindows =
                    viewModel.StartWithWindows;

                _startupScreenshotTranslationHotkey =
                    viewModel.ScreenshotTranslationHotkey;

                _startupScreenshotSourceLanguage =
                    viewModel.ScreenshotSourceLanguage;

                _startupScreenshotTargetLanguage =
                    viewModel.ScreenshotTargetLanguage;
            };

        return window.ShowDialog() == true;
    }

    public async void ShowSettings()
    {
        if (
            _firstRunSettingsPersistence is null ||
            _startWithWindowsSettingsPersistence is null ||
            _apiKeyValidator is null ||
            _settingsService is null ||
            _textTranslationHotkeyPersistence is null ||
            _screenshotTranslationHotkeyPersistence is null ||
            _screenshotLanguageSettingsPersistence is null ||
            _hotkeyService is null
        )
        {
            return;
        }

        AppSettings currentSettings;

        try
        {
            currentSettings =
                await _settingsService.LoadAsync();
        }
        catch
        {
            System.Windows.MessageBox.Show(
                "读取设置失败。",
                "语桥"
            );

            return;
        }

        var hotkeyChangeService =
            new TextTranslationHotkeyChangeService(
                _hotkeyService,
                _textTranslationHotkeyPersistence
            );

        var screenshotHotkeyChangeService =
            new ScreenshotTranslationHotkeyChangeService(
                _hotkeyService,
                _screenshotTranslationHotkeyPersistence
            );

        var viewModel =
            new FirstRunSettingsViewModel(
                _firstRunSettingsPersistence,
                _startWithWindowsSettingsPersistence,
                _apiKeyValidator,
                hotkeyChangeService:
                    hotkeyChangeService,
                currentTextTranslationHotkey:
                    currentSettings.TextTranslationHotkey,
                screenshotLanguagePersistence:
                    _screenshotLanguageSettingsPersistence,
                currentScreenshotSourceLanguage:
                    currentSettings.ScreenshotSourceLanguage,
                currentScreenshotTargetLanguage:
                    currentSettings.ScreenshotTargetLanguage,
                screenshotHotkeyChangeService:
                    screenshotHotkeyChangeService,
                currentScreenshotTranslationHotkey:
                    currentSettings.ScreenshotTranslationHotkey
            )
            {
                StartWithWindows =
                    _startWithWindows
            };

        var window =
            new FirstRunSettingsWindow(
                viewModel,
                isFirstRun: false,
                hotkeyService: _hotkeyService
            );

        var hotkeyService =
            _hotkeyService;

        hotkeyService.SuspendRequests();

        window.Closed +=
            (_, _) =>
            {
                _startWithWindows =
                    viewModel.StartWithWindows;

                hotkeyService.ResumeRequests();
            };

        try
        {
            window.Show();
        }
        catch
        {
            hotkeyService.ResumeRequests();
            throw;
        }
    }

    public void ShowScreenshotTranslation()
    {
        _screenshotTranslationCoordinator?
            .Toggle();
    }

    protected override void OnExit(
        System.Windows.ExitEventArgs e
    )
    {
        _screenshotTranslationCoordinator?.Dispose();

        if (
            _hotkeyService is not null &&
            _windowManager is not null
        )
        {
            _hotkeyService.TextTranslationRequested -=
                _windowManager.ToggleTranslateWindow;
        }

        if (
            _hotkeyService is not null &&
            _appController is not null
        )
        {
            _hotkeyService.ScreenshotTranslationRequested -=
                _appController.OpenScreenshotTranslation;
        }

        _hotkeyBackend?.Unregister(
            HotkeyService.TextTranslationHotkeyId
        );

        _hotkeyBackend?.Unregister(
            HotkeyService.ScreenshotTranslationHotkeyId
        );

        _hotkeyMessageWindow?.Dispose();

        if (_trayService is not null)
        {
            _trayService.TextTranslationRequested -=
                OnTrayTextTranslationRequested;

            _trayService.SettingsRequested -=
                OnTraySettingsRequested;

            _trayService.ExitRequested -=
                OnTrayExitRequested;

            _trayService.ScreenshotTranslationRequested -=
                OnTrayScreenshotTranslationRequested;

            _trayService.Dispose();
        }

        _httpClient?.Dispose();

        base.OnExit(e);
    }
}
