using LightTranslator.Services.Logging;
using System.Net.Http;

using LightTranslator.Controllers;
using LightTranslator.Infrastructure.Security;
using LightTranslator.Services.Hotkeys;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;
using LightTranslator.Services.Tray;
using LightTranslator.Services.Windows;
using LightTranslator.ViewModels;
using LightTranslator.Views;
using LightTranslator.Services.Startup;
using LightTranslator.Models;

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


    protected override async void OnStartup(
        System.Windows.StartupEventArgs e
    )
    {
        base.OnStartup(e);


        // 设置服务
        var settingsService =
            new SettingsService();

        _settingsService =
            settingsService;

        _textTranslationHotkeyPersistence =
            new TextTranslationHotkeySettingsPersistence(
                settingsService
            );


        // API Key 安全存储
        _secretStorage =
            new DpapiSecretStorage();


        // 首次设置持久化
        _firstRunSettingsPersistence =
            new FirstRunSettingsPersistence(
                _secretStorage,
                settingsService
            );


        // 读取应用设置
        var settings =
            await settingsService.LoadAsync();
        _startupTextTranslationHotkey =
            settings.TextTranslationHotkey;

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

        // HTTP
        _httpClient =
            new HttpClient();


        // API Key 验证
        _apiKeyValidator =
            new DeepSeekApiKeyValidator(
                _httpClient
            );


        // 首次启动判断
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

        // 首次设置窗口可能修改了配置，
        // 继续启动前重新读取最新设置。
        settings =
            await settingsService.LoadAsync();

        _startupTextTranslationHotkey =
            settings.TextTranslationHotkey;

        _startWithWindows =
            settings.StartWithWindows;


        // 翻译服务
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


        // 翻译窗口管理
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


        // 热键消息路由
        var router =
            new HotkeyMessageRouter();


        // 隐藏消息窗口
        _hotkeyMessageWindow =
            new HotkeyMessageWindow(
                router
            );


        // Windows 热键 API
        var nativeApi =
            new User32HotkeyNativeApi();


        // Win32 热键后端
        _hotkeyBackend =
            new Win32HotkeyBackend(
                _hotkeyMessageWindow.Handle,
                nativeApi,
                router
            );


        // 热键服务
        _hotkeyService =
            new HotkeyService(
                _hotkeyBackend
            );


        // Alt + T
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
                "LightTranslator"
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
                "LightTranslator"
            );

            Shutdown();
            return;
        }


        // 托盘
        var trayBackend =
            new NotifyIconTrayBackend();


        _trayService =
            new TrayService(
                trayBackend
            );


        _trayService.SettingsRequested +=
            OnTraySettingsRequested;


        _trayService.ExitRequested +=
            OnTrayExitRequested;

        _trayService.ScreenshotTranslationRequested +=
            OnTrayScreenshotTranslationRequested;


        _trayService.Start();
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
            _startupTextTranslationHotkey is null
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
                    _startupTextTranslationHotkey
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
                "LightTranslator"
            );

            return;
        }

        var hotkeyChangeService =
            new TextTranslationHotkeyChangeService(
                _hotkeyService,
                _textTranslationHotkeyPersistence
            );

        var viewModel =
            new FirstRunSettingsViewModel(
                _firstRunSettingsPersistence,
                _startWithWindowsSettingsPersistence,
                _apiKeyValidator,
                hotkeyChangeService:
                    hotkeyChangeService,
                currentTextTranslationHotkey:
                    currentSettings.TextTranslationHotkey
            )
            {
                StartWithWindows =
                    _startWithWindows
            };

        var window =
            new FirstRunSettingsWindow(
                viewModel,
                isFirstRun: false
            );

        window.Closed +=
            (_, _) =>
            {
                _startWithWindows =
                    viewModel.StartWithWindows;
            };

        window.Show();
    }

    public void ShowScreenshotTranslation()
    {
        var window =
            new ScreenshotTranslationWindow();

        window.Show();
    }


    protected override void OnExit(
        System.Windows.ExitEventArgs e
    )
    {
        // 解绑热键事件
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

        // 注销系统热键
        _hotkeyBackend?.Unregister(
            HotkeyService.TextTranslationHotkeyId
        );
        _hotkeyBackend?.Unregister(
            HotkeyService.ScreenshotTranslationHotkeyId
        );


        // 释放热键消息窗口
        _hotkeyMessageWindow?.Dispose();


        // 释放托盘
        if (_trayService is not null)
        {
            _trayService.SettingsRequested -=
                OnTraySettingsRequested;


            _trayService.ExitRequested -=
                OnTrayExitRequested;

            _trayService.ScreenshotTranslationRequested -=
                OnTrayScreenshotTranslationRequested;


            _trayService.Dispose();
        }


        // 释放 HttpClient
        _httpClient?.Dispose();


        base.OnExit(e);
    }
}