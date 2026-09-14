using LightTranslator.ViewModels;
using LightTranslator.Views;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public class FirstRunSettingsWindowTests
{

    [Fact]
    public void ApiKeyChangedAfterValidation_ClearsDisplayedTestMessage()
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var validator =
                            new FakeApiKeyValidator();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                apiKeyValidator:
                                    validator
                            );

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: true
                            );

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var testButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "TestApiKeyButton"
                            );

                        var messageTextBlock =
                            (System.Windows.Controls.TextBlock)
                            window.FindName(
                                "ApiKeyTestMessageTextBlock"
                            );

                        passwordBox.Password =
                            "key-a";

                        testButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle
                        );

                        Assert.Equal(
                            "连接成功",
                            messageTextBlock.Text
                        );

                        passwordBox.Password =
                            "key-b";

                        Assert.Equal(
                            string.Empty,
                            messageTextBlock.Text
                        );

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
    }

    [Fact]
    public void TestApiKeyButtonClick_WhenValidationSucceeds_ShowsSuccessMessage()
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var validator =
                            new FakeApiKeyValidator();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                apiKeyValidator:
                                    validator
                            );

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: true
                            );

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var testButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "TestApiKeyButton"
                            );

                        var messageTextBlock =
                            (System.Windows.Controls.TextBlock)
                            window.FindName(
                                "ApiKeyTestMessageTextBlock"
                            );

                        passwordBox.Password =
                            "test-key";

                        testButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle
                        );

                        Assert.Equal(
                            "连接成功",
                            messageTextBlock.Text
                        );

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
    }

    [Fact]
    public void NormalMode_TestApiKeyFails_KeepsSaveButtonDisabled()
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var validator =
                            new FailingApiKeyValidator();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                apiKeyValidator:
                                    validator
                            );

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: false
                            );

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var testButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "TestApiKeyButton"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "invalid-key";

                        Assert.False(
                            saveButton.IsEnabled
                        );

                        testButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle
                        );

                        Assert.False(
                            viewModel.ApiKeyTestSucceeded
                        );

                        Assert.False(
                            saveButton.IsEnabled
                        );

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
    }

    [Fact]
    public void NormalMode_ApiKeyChangedWithoutValidation_DisablesSaveButton()
    {
        Exception? exception =
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

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
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

                        Assert.True(
                            saveButton.IsEnabled
                        );

                        passwordBox.Password =
                            "new-test-key";

                        Assert.False(
                            saveButton.IsEnabled
                        );

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
    }

    [Fact]
    public void TestApiKeyButtonClick_WhenValidationSucceeds_EnablesSaveButton()
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var validator =
                            new FakeApiKeyValidator();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                apiKeyValidator:
                                    validator
                            );

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: true
                            );

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var testButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "TestApiKeyButton"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "test-key";

                        testButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.Controls.Button.ClickEvent
                            )
                        );

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.ApplicationIdle
                        );

                        Assert.Equal(
                            "test-key",
                            validator.ValidatedApiKey
                        );

                        Assert.True(
                            viewModel.ApiKeyTestSucceeded
                        );

                        Assert.True(
                            saveButton.IsEnabled
                        );

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
    }

    [Fact]
    public void FirstRunMode_SaveButtonClick_ReturnsTrueDialogResult()
    {
        Exception? exception =
            null;

        bool? dialogResult =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var firstRunPersistence =
                            new FakeFirstRunSettingsPersistence();

                        var startupPersistence =
                            new FakeStartWithWindowsSettingsPersistence();

                        var viewModel =
                            new FirstRunSettingsViewModel(
                                firstRunPersistence,
                                startupPersistence
                            )
                            {
                                ApiKey =
                                    "test-key",

                                ApiKeyTestSucceeded =
                                    true
                            };

                        var window =
                            new FirstRunSettingsWindow(
                                viewModel,
                                isFirstRun: true,
                                isDialogMode: true
                            );

                        window.Loaded +=
                            (_, _) =>
                            {
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
                            };

                        dialogResult =
                            window.ShowDialog();
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
            dialogResult == true
        );
    }

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

                        viewModel.ApiKeyTestSucceeded =
                            true;

                        saveButton.IsEnabled =
                            viewModel.CanSave;

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
    public void ApiKeyChanged_EnablesTestButtonButKeepsSaveDisabled()
    {
        Exception? exception =
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
                                isFirstRun: true
                            );

                        window.Show();

                        window.Dispatcher.Invoke(
                            () =>
                            {
                            },
                            System.Windows.Threading.DispatcherPriority.DataBind
                        );

                        var passwordBox =
                            (System.Windows.Controls.PasswordBox)
                            window.FindName(
                                "ApiKeyPasswordBox"
                            );

                        var testButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "TestApiKeyButton"
                            );

                        var saveButton =
                            (System.Windows.Controls.Button)
                            window.FindName(
                                "SaveButton"
                            );

                        passwordBox.Password =
                            "test-key";

                        Assert.Equal(
                            "test-key",
                            viewModel.ApiKey
                        );

                        Assert.True(
                            testButton.IsEnabled
                        );

                        Assert.False(
                            saveButton.IsEnabled
                        );

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

    private sealed class FakeApiKeyValidator
        : IApiKeyValidator
    {
        public string? ValidatedApiKey { get; private set; }

        public Task ValidateAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            ValidatedApiKey =
                apiKey;

            return Task.CompletedTask;
        }
    }

    private sealed class FailingApiKeyValidator
        : IApiKeyValidator
    {
        public Task ValidateAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromException(
                new InvalidOperationException(
                    "validation failed"
                )
            );
        }
    }
}