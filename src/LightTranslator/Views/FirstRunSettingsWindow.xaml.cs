using LightTranslator.ViewModels;

namespace LightTranslator.Views;

public partial class FirstRunSettingsWindow
    : System.Windows.Window
{
    private readonly FirstRunSettingsViewModel _viewModel;
    private readonly bool _isFirstRun;
    private readonly bool _isDialogMode;

    public FirstRunSettingsWindow(
        FirstRunSettingsViewModel viewModel,
        bool isFirstRun = true,
        bool isDialogMode = false
    )
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        _isFirstRun =
            isFirstRun;

        _isDialogMode =
            isDialogMode;

        DataContext =
            viewModel;

        TitleTextBlock.Text =
            isFirstRun
                ? "首次设置"
                : "设置";

        SaveButton.IsEnabled =
            !_isFirstRun ||
            _viewModel.CanSave;
    }

    private void OnApiKeyPasswordChanged(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        _viewModel.ApiKey =
            ApiKeyPasswordBox.Password;

        SaveButton.IsEnabled =
            !_isFirstRun ||
            _viewModel.CanSave;
    }

    private async void OnSaveClick(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
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
                    DialogResult =
                        true;
                }
                else
                {
                    Close();
                }
            }

            return;
        }

        // 普通设置：
        // API Key 留空表示“不修改现有 Key”
        var normalApiKeySaved =
            true;

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