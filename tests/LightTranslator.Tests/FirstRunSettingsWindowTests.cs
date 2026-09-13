using LightTranslator.ViewModels;
using LightTranslator.Views;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class FirstRunSettingsWindowTests
{

    [Fact]
    public void SaveButtonClick_SavesApiKey()
    {
        Exception? exception =
            null;

        string? savedApiKey =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var persistence =
                            new FakeFirstRunSettingsPersistence();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                persistence
                            );

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel
                            );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "test-key";

                        saveButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        savedApiKey =
                            persistence.SavedApiKey;
                    }
                    catch (Exception ex)
                    {
                        exception =
                            ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );

        Assert.Equal(
            "test-key",
            savedApiKey
        );
    }

    [Fact]
    public void ApiKeyChanged_UpdatesViewModelAndEnablesSaveButton()
    {
        Exception? exception =
            null;

        string? apiKey =
            null;

        bool saveButtonEnabled =
            false;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var viewModel =
                            new FirstRunSettingsViewModel();

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel
                            );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "test-key";

                        apiKey =
                            viewModel.ApiKey;

                        saveButtonEnabled =
                            saveButton.IsEnabled;

                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        exception =
                            ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );

        Assert.Equal(
            "test-key",
            apiKey
        );

        Assert.True(
            saveButtonEnabled
        );
    }

    [Fact]
    public void Constructor_SetsViewModelAsDataContext()
    {
        Exception? exception =
            null;

        object? dataContext =
            null;

        FirstRunSettingsViewModel? viewModel =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        viewModel =
                            new FirstRunSettingsViewModel();

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel
                            );

                        dataContext =
                            window.DataContext;

                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        exception =
                            ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );

        Assert.Same(
            viewModel,
            dataContext
        );
    }

    private sealed class FakeFirstRunSettingsPersistence
        : IFirstRunSettingsPersistence
    {
        public string? SavedApiKey { get; private set; }

        public Task SaveAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            SavedApiKey =
                apiKey;

            return Task.CompletedTask;
        }
    }
}