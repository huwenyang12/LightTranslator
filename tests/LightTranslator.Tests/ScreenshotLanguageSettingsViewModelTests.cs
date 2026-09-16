using System.Windows.Controls;
using System.Windows.Threading;
using LightTranslator.Services.Settings;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class ScreenshotLanguageSettingsViewModelTests
{
    [Fact]
    public void ViewModel_ExposesUniversalModelLanguagesAndCurrentSelection()
    {
        var viewModel =
            new FirstRunSettingsViewModel(
                currentScreenshotSourceLanguage:
                    "ja",
                currentScreenshotTargetLanguage:
                    "en"
            );

        Assert.Equal(
            new[]
            {
                "auto",
                "zh",
                "en",
                "ja"
            },
            viewModel.ScreenshotSourceLanguages
                .Select(
                    language =>
                        language.Code
                )
        );

        Assert.Equal(
            new[]
            {
                "zh",
                "en",
                "ja"
            },
            viewModel.ScreenshotTargetLanguages
                .Select(
                    language =>
                        language.Code
                )
        );

        Assert.Equal(
            "ja",
            viewModel.ScreenshotSourceLanguage
        );

        Assert.Equal(
            "en",
            viewModel.ScreenshotTargetLanguage
        );
    }

    [Fact]
    public async Task SaveScreenshotLanguagesAsync_UsesSelectedValues()
    {
        var persistence =
            new FakeScreenshotLanguageSettingsPersistence();

        var viewModel =
            new FirstRunSettingsViewModel(
                screenshotLanguagePersistence:
                    persistence
            )
            {
                ScreenshotSourceLanguage =
                    "en",

                ScreenshotTargetLanguage =
                    "ja"
            };

        var saved =
            await viewModel.SaveScreenshotLanguagesAsync();

        Assert.True(
            saved
        );

        Assert.Equal(
            "en",
            persistence.SourceLanguage
        );

        Assert.Equal(
            "ja",
            persistence.TargetLanguage
        );
    }

    [Fact]
    public void Window_BindsScreenshotLanguageSelectors()
    {
        RunOnSta(
            () =>
            {
                var viewModel =
                    new FirstRunSettingsViewModel(
                        currentScreenshotSourceLanguage:
                            "ja",
                        currentScreenshotTargetLanguage:
                            "en"
                    );

                var window =
                    new FirstRunSettingsWindow(
                        viewModel,
                        isFirstRun: false
                    );

                try
                {
                    window.Show();

                    window.Dispatcher.Invoke(
                        () =>
                        {
                        },
                        DispatcherPriority.DataBind
                    );

                    var sourceSelector =
                        Assert.IsType<ComboBox>(
                            window.FindName(
                                "ScreenshotSourceLanguageComboBox"
                            )
                        );

                    var targetSelector =
                        Assert.IsType<ComboBox>(
                            window.FindName(
                                "ScreenshotTargetLanguageComboBox"
                            )
                        );

                    Assert.Equal(
                        "ja",
                        sourceSelector.SelectedValue
                    );

                    Assert.Equal(
                        "en",
                        targetSelector.SelectedValue
                    );

                    Assert.Equal(
                        4,
                        sourceSelector.Items.Count
                    );

                    Assert.Equal(
                        3,
                        targetSelector.Items.Count
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    private static void RunOnSta(
        Action action
    )
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception caught)
                    {
                        exception =
                            caught;
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

    private sealed class FakeScreenshotLanguageSettingsPersistence
        : IScreenshotLanguageSettingsPersistence
    {
        public string? SourceLanguage
        {
            get;
            private set;
        }

        public string? TargetLanguage
        {
            get;
            private set;
        }

        public Task SaveAsync(
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken = default
        )
        {
            SourceLanguage =
                sourceLanguage;

            TargetLanguage =
                targetLanguage;

            return
                Task.CompletedTask;
        }
    }
}
