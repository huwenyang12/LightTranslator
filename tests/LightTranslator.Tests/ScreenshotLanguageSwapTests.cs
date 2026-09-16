using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class ScreenshotLanguageSwapTests
{
    [Fact]
    public void SwapScreenshotLanguages_ExplicitLanguages_SwapsSourceAndTarget()
    {
        var viewModel = new FirstRunSettingsViewModel(
            currentScreenshotSourceLanguage: "en",
            currentScreenshotTargetLanguage: "zh"
        );

        var swapped = viewModel.SwapScreenshotLanguages();

        Assert.True(swapped);
        Assert.Equal("zh", viewModel.ScreenshotSourceLanguage);
        Assert.Equal("en", viewModel.ScreenshotTargetLanguage);
    }

    [Fact]
    public void SwapScreenshotLanguages_AutoSource_DoesNotSwap()
    {
        var viewModel = new FirstRunSettingsViewModel(
            currentScreenshotSourceLanguage: "auto",
            currentScreenshotTargetLanguage: "zh"
        );

        var swapped = viewModel.SwapScreenshotLanguages();

        Assert.False(swapped);
        Assert.Equal("auto", viewModel.ScreenshotSourceLanguage);
        Assert.Equal("zh", viewModel.ScreenshotTargetLanguage);
    }

    [Fact]
    public void SettingsWindow_ContainsScreenshotLanguageSwapButton()
    {
        RunOnSta(
            () =>
            {
                var window = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(),
                    isFirstRun: false
                );

                var button = window.FindName(
                    "ScreenshotLanguageSwapButton"
                );

                Assert.NotNull(button);
                window.Close();
            }
        );
    }

    [Fact]
    public void SettingsWindow_SwapButtonDisabledForAutoAndEnabledForExplicitSource()
    {
        RunOnSta(
            () =>
            {
                var autoWindow = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(
                        currentScreenshotSourceLanguage: "auto",
                        currentScreenshotTargetLanguage: "zh"
                    ),
                    isFirstRun: false
                );

                var autoButton =
                    (System.Windows.Controls.Button)
                    autoWindow.FindName(
                        "ScreenshotLanguageSwapButton"
                    );

                Assert.False(autoButton.IsEnabled);
                autoWindow.Close();

                var explicitWindow = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(
                        currentScreenshotSourceLanguage: "en",
                        currentScreenshotTargetLanguage: "zh"
                    ),
                    isFirstRun: false
                );

                var explicitButton =
                    (System.Windows.Controls.Button)
                    explicitWindow.FindName(
                        "ScreenshotLanguageSwapButton"
                    );

                Assert.True(explicitButton.IsEnabled);
                explicitWindow.Close();
            }
        );
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
        );

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }
}
