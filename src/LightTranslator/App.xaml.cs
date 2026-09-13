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

namespace LightTranslator;

public partial class App
    : System.Windows.Application,
      IAppStartupView
{
    private HttpClient? _httpClient;

    private ISecretStorage? _secretStorage;

    private IFirstRunSettingsPersistence? _firstRunSettingsPersistence;

    private HotkeyMessageWindow? _hotkeyMessageWindow;

    private Win32HotkeyBackend? _hotkeyBackend;

    private HotkeyService? _hotkeyService;

    private WindowManager? _windowManager;

    private TrayService? _trayService;


    protected override async void OnStartup(
        System.Windows.StartupEventArgs e
    )
    {
        base.OnStartup(e);


        // 设置服务
        var settingsService =
            new SettingsService();


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


        // 首次启动判断
        var appController =
            new AppController(
                this
            );

        appController.Start(
            settings
        );


        // HTTP
        _httpClient =
            new HttpClient();


        // 翻译服务
        var translationService =
            new DeepSeekTranslationService(
                _httpClient,
                () =>
                    _secretStorage.Load(
                        "deepseek-api-key"
                    )
            );


        // 翻译窗口管理
        _windowManager =
            new WindowManager(
                () =>
                    new TranslateWindow(
                        new TranslateViewModel(
                            translationService
                        )
                    )
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


        _trayService.Start();
    }


    private void OnTraySettingsRequested()
    {
        ShowFirstRunSettings();
    }


    private void OnTrayExitRequested()
    {
        Shutdown();
    }


    public void ShowFirstRunSettings()
    {
        if (_firstRunSettingsPersistence is null)
        {
            return;
        }


        var viewModel =
            new FirstRunSettingsViewModel(
                _firstRunSettingsPersistence
            );


        var window =
            new FirstRunSettingsWindow(
                viewModel
            );


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


        // 注销系统热键
        _hotkeyBackend?.Unregister(
            HotkeyService.TextTranslationHotkeyId
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


            _trayService.Dispose();
        }


        // 释放 HttpClient
        _httpClient?.Dispose();


        base.OnExit(e);
    }
}