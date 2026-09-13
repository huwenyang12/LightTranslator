using LightTranslator.ViewModels;
using LightTranslator.Views;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class FirstRunSettingsWindowTests
{

    [Fact]
    public void SaveButtonClick_SavesStartWithWindows()
    {
        Exception? exception =
            null;

        bool? savedStartWithWindows =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var apiKeyPersistence =
                            new FakeFirstRunSettingsPersistence();

                        var startupPersistence =
                            new FakeStartWithWindowsSettingsPersistence();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                apiKeyPersistence,
                                startupPersistence
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

                        var checkBox =
                            (System.Windows.Controls.CheckBox)
                            window.FindName(
                                "StartWithWindowsCheckBox"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "test-key";

                        viewModel.StartWithWindows =
                            true;

                        saveButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        savedStartWithWindows =
                            startupPersistence.SavedValue;
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

        Assert.True(
            savedStartWithWindows
        );
    }

    [Fact]
    public void Constructor_ShowsCurrentStartWithWindowsValue()
    {
        Exception? exception =
            null;

        bool? isChecked =
            null;

        bool viewModelValue =
            false;

        bool bindingExists =
            false;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var viewModel =
                            new FirstRunSettingsViewModel
                            {
                                StartWithWindows = true
                            };

                        viewModelValue =
                            viewModel.StartWithWindows;

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: false
                            );

                        var checkBox =
                            (System.Windows.Controls.CheckBox)
                            window.FindName(
                                "StartWithWindowsCheckBox"
                            );

                        var bindingExpression =
                            checkBox.GetBindingExpression(
                                System.Windows.Controls.CheckBox.IsCheckedProperty
                            );

                        bindingExists =
                            bindingExpression is not null;

                        // 真正让 Window 进入 Loaded 状态
                        window.Show();

                        // 让 WPF 处理 DataBinding 队列
                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        isChecked =
                            checkBox.IsChecked;

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

        Assert.True(
            viewModelValue
        );

        Assert.True(
            bindingExists
        );

        Assert.True(
            isChecked
        );
    }


    [Fact]
    public void NormalMode_SaveButtonIsEnabledWithoutApiKey()
    {
        Exception? exception =
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
                                viewModel,
                                isFirstRun: false
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

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

        Assert.True(
            saveButtonEnabled
        );
    }


    [Fact]
    public void NormalMode_SaveWithoutApiKey_SavesStartWithWindowsAndCloses()
    {
        Exception? exception =
            null;

        bool? savedStartWithWindows =
            null;

        bool windowClosed =
            false;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var startupPersistence =
                            new FakeStartWithWindowsSettingsPersistence();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                startupPersistence:
                                    startupPersistence
                            )
                            {
                                StartWithWindows = true
                            };

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: false
                            );

                        window.Closed +=
                            (_, _) =>
                                windowClosed = true;

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        saveButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        savedStartWithWindows =
                            startupPersistence.SavedValue;
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

        Assert.True(
            savedStartWithWindows
        );

        Assert.True(
            windowClosed
        );
    }

    [Fact]
    public void NormalMode_ShowsSettingsTitle()
    {
        Exception? exception =
            null;

        string? titleText =
            null;

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
                                viewModel,
                                isFirstRun: false
                            );

                        var titleBlock =
                            (System.Windows.Controls.TextBlock)
                            window.FindName(
                                "TitleTextBlock"
                            );

                        titleText =
                            titleBlock.Text;

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
            "设置",
            titleText
        );
    }

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

    private sealed class FakeStartWithWindowsSettingsPersistence
        : IStartWithWindowsSettingsPersistence
    {
        public bool? SavedValue { get; private set; }

        public Task SaveAsync(
            bool enabled,
            CancellationToken cancellationToken = default
        )
        {
            SavedValue =
                enabled;

            return Task.CompletedTask;
        }
    }
}