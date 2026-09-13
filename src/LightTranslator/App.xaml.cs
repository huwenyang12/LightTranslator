using System.Net.Http;
using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.Services.Translation;
using LightTranslator.Services.Windows;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator;

public partial class App
    : System.Windows.Application
{
    private HttpClient? _httpClient;

    private HotkeyMessageWindow? _hotkeyMessageWindow;

    private Win32HotkeyBackend? _hotkeyBackend;

    private HotkeyService? _hotkeyService;

    private WindowManager? _windowManager;

    protected override void OnStartup(
        System.Windows.StartupEventArgs e
    )
    {
        base.OnStartup(e);

        _httpClient =
            new HttpClient();

        var translationService =
            new DeepSeekTranslationService(
                _httpClient,
                () => null
            );

        _windowManager =
            new WindowManager(
                () =>
                    new TranslateWindow(
                        new TranslateViewModel(
                            translationService
                        )
                    )
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

        var hotkey =
            AppSettings
                .CreateDefault()
                .TextTranslationHotkey;

        var registered =
            _hotkeyService.RegisterTextTranslation(
                hotkey
            );

        if (!registered)
        {
            System.Windows.MessageBox.Show(
                "Alt+T 全局快捷键注册失败，可能已被其他程序占用。",
                "LightTranslator"
            );

            Shutdown();
        }
    }

    protected override void OnExit(
        System.Windows.ExitEventArgs e
    )
    {
        if (
            _hotkeyService is not null &&
            _windowManager is not null
        )
        {
            _hotkeyService.TextTranslationRequested -=
                _windowManager.ToggleTranslateWindow;
        }

        _hotkeyBackend?.Unregister(
            HotkeyService.TextTranslationHotkeyId
        );

        _hotkeyMessageWindow?.Dispose();

        _httpClient?.Dispose();

        base.OnExit(e);
    }
}