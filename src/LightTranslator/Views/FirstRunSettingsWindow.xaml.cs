using LightTranslator.ViewModels;

namespace LightTranslator.Views;

public partial class FirstRunSettingsWindow
    : System.Windows.Window
{
    private readonly FirstRunSettingsViewModel _viewModel;

    public FirstRunSettingsWindow(
        FirstRunSettingsViewModel viewModel,
        bool isFirstRun = true
    )
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;

        TitleTextBlock.Text =
            isFirstRun
                ? "首次设置"
                : "设置";
    }

    private void OnApiKeyPasswordChanged(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        _viewModel.ApiKey =
            ApiKeyPasswordBox.Password;

        SaveButton.IsEnabled =
            _viewModel.CanSave;
    }

    private async void OnSaveClick(
        object sender,
        System.Windows.RoutedEventArgs e
    )
    {
        var saved =
            await _viewModel.SaveAsync();

        if (saved)
        {
            Close();
        }
    }
}