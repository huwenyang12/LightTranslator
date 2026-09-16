using LightTranslator.ViewModels;
using System.Collections.Generic;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Views;

public partial class FirstRunSettingsWindow
    : System.Windows.Window
{
    private readonly FirstRunSettingsViewModel _viewModel;
    private readonly bool _isFirstRun;
    private readonly bool _isDialogMode;

    private readonly Func<System.Windows.Input.ModifierKeys>
        _modifierKeysProvider;

    public FirstRunSettingsWindow(
        FirstRunSettingsViewModel viewModel,
        bool isFirstRun = true,
        bool isDialogMode = false,
        Func<System.Windows.Input.ModifierKeys>? modifierKeysProvider = null
    )
    {
        InitializeComponent();

        _viewModel = viewModel;
        _isFirstRun = isFirstRun;
        _isDialogMode = isDialogMode;

        DataContext = viewModel;

        TitleTextBlock.Text =
            isFirstRun
                ? "首次设置"
                : "设置";

        TextTranslationHotkeyBox.Text =
            FormatHotkey(
                _viewModel.TextTranslationHotkey
            );

        ScreenshotTranslationHotkeyBox.Text =
            FormatHotkey(
                _viewModel.ScreenshotTranslationHotkey
            );

        SaveButton.IsEnabled =
            !_isFirstRun ||
            _viewModel.CanSave;

        _modifierKeysProvider =
            modifierKeysProvider
            ?? (() => System.Windows.Input.Keyboard.Modifiers);
    }

    private static string FormatHotkey(
        LightTranslator.Models.HotkeyDefinition? hotkey
    )
    {
        if (hotkey is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (hotkey.Control)
        {
            parts.Add("Ctrl");
        }

        if (hotkey.Alt)
        {
            parts.Add("Alt");
        }

        if (hotkey.Shift)
        {
            parts.Add("Shift");
        }

        if (hotkey.Windows)
        {
            parts.Add("Win");
        }

        parts.Add(hotkey.Key);

        return string.Join(
            " + ",
            parts
        );
    }

    private void OnTextTranslationHotkeyBoxPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            TextTranslationHotkeyBox.Text =
                FormatHotkey(
                    _viewModel.TextTranslationHotkey
                );

            e.Handled = true;
            return;
        }

        var captured = HotkeyCaptureParser.TryCapture(
            e.Key,
            e.SystemKey,
            _modifierKeysProvider(),
            out var hotkey
        );

        if (!captured || hotkey is null)
        {
            return;
        }

        _viewModel.SetTextTranslationHotkey(hotkey);
        TextTranslationHotkeyBox.Text = FormatHotkey(hotkey);
        e.Handled = true;
    }

    private void OnScreenshotTranslationHotkeyBoxPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            ScreenshotTranslationHotkeyBox.Text =
                FormatHotkey(
                    _viewModel.ScreenshotTranslationHotkey
                );

            e.Handled = true;
            return;
        }

        var captured = HotkeyCaptureParser.TryCapture(
            e.Key,
            e.SystemKey,
            _modifierKeysProvider(),
            out var hotkey
        );

        if (!captured || hotkey is null)
        {
            return;
        }

        _viewModel.SetScreenshotTranslationHotkey(hotkey);
        ScreenshotTranslationHotkeyBox.Text = FormatHotkey(hotkey);
        e.Handled = true;
    }

    private void OnApiKeyPasswordChanged(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        _viewModel.ApiKey = ApiKeyPasswordBox.Password;
        ApiKeyTestMessageTextBlock.Text = _viewModel.ApiKeyTestMessage;

        TestApiKeyButton.IsEnabled =
            !string.IsNullOrWhiteSpace(_viewModel.ApiKey) &&
            !_viewModel.IsTestingApiKey;

        SaveButton.IsEnabled =
            _isFirstRun
                ? _viewModel.CanSave
                : string.IsNullOrWhiteSpace(_viewModel.ApiKey) ||
                  _viewModel.CanSave;
    }

    private async void OnTestApiKeyClick(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        TestApiKeyButton.IsEnabled = false;

        await _viewModel.TestApiKeyAsync();

        ApiKeyTestMessageTextBlock.Text = _viewModel.ApiKeyTestMessage;

        TestApiKeyButton.IsEnabled =
            !string.IsNullOrWhiteSpace(_viewModel.ApiKey) &&
            !_viewModel.IsTestingApiKey;

        SaveButton.IsEnabled =
            _isFirstRun
                ? _viewModel.CanSave
                : string.IsNullOrWhiteSpace(_viewModel.ApiKey) ||
                  _viewModel.CanSave;
    }

    private void OnTextTranslationHotkeyBoxGotFocus(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        TextTranslationHotkeyBox.Text = "请按快捷键";
    }

    private void OnScreenshotTranslationHotkeyBoxGotFocus(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        ScreenshotTranslationHotkeyBox.Text = "请按快捷键";
    }

    private async void OnSaveClick(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        var hotkeySaved =
            await _viewModel.SaveTextTranslationHotkeyAsync();

        TextTranslationHotkeyErrorTextBlock.Text =
            hotkeySaved
                ? string.Empty
                : "快捷键注册失败，可能已被其他程序占用";

        if (!hotkeySaved)
        {
            TextTranslationHotkeyBox.Text =
                FormatHotkey(
                    _viewModel.TextTranslationHotkey
                );

            return;
        }

        var screenshotHotkeySaved =
            await _viewModel.SaveScreenshotTranslationHotkeyAsync();

        ScreenshotTranslationHotkeyErrorTextBlock.Text =
            screenshotHotkeySaved
                ? string.Empty
                : "快捷键注册失败，可能已被其他程序占用";

        if (!screenshotHotkeySaved)
        {
            ScreenshotTranslationHotkeyBox.Text =
                FormatHotkey(
                    _viewModel.ScreenshotTranslationHotkey
                );

            return;
        }

        var screenshotLanguagesSaved =
            await _viewModel.SaveScreenshotLanguagesAsync();

        if (!screenshotLanguagesSaved)
        {
            return;
        }

        if (_isFirstRun)
        {
            var firstRunApiKeySaved =
                await _viewModel.SaveAsync();

            var firstRunStartupSaved =
                await _viewModel.SaveStartWithWindowsAsync();

            if (
                firstRunApiKeySaved &&
                firstRunStartupSaved
            )
            {
                if (_isDialogMode)
                {
                    DialogResult = true;
                }
                else
                {
                    Close();
                }
            }

            return;
        }

        var normalApiKeySaved = true;

        if (_viewModel.CanSave)
        {
            normalApiKeySaved =
                await _viewModel.SaveAsync();
        }

        var normalStartupSaved =
            await _viewModel.SaveStartWithWindowsAsync();

        if (
            normalApiKeySaved &&
            normalStartupSaved
        )
        {
            Close();
        }
    }
}
