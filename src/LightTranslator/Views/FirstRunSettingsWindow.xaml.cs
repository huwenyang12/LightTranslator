using LightTranslator.ViewModels;

namespace LightTranslator.Views;

public partial class FirstRunSettingsWindow
    : System.Windows.Window
{
    private readonly FirstRunSettingsViewModel _viewModel;

    public FirstRunSettingsWindow(
        FirstRunSettingsViewModel viewModel
    )
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;
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