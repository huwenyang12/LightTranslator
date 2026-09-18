using System.Windows.Controls;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class FirstRunSettingsWindowVisualTests
{
    [Theory]
    [InlineData(true, "首次设置")]
    [InlineData(false, "设置")]
    public void Window_UsesGroupedCardsAndPreservesNamedControls(
        bool isFirstRun,
        string expectedTitle
    )
    {
        RunOnSta(
            () =>
            {
                var window =
                    new FirstRunSettingsWindow(
                        new FirstRunSettingsViewModel(),
                        isFirstRun
                    );

                Assert.Equal(
                    560d,
                    window.Width
                );
                Assert.Equal(
                    720d,
                    window.Height
                );

                Assert.Equal(
                    expectedTitle,
                    Assert.IsType<TextBlock>(
                        window.FindName(
                            "TitleTextBlock"
                        )
                    ).Text
                );

                foreach (
                    var cardName in new[]
                    {
                        "AccountSettingsCard",
                        "TextTranslationSettingsCard",
                        "ScreenshotTranslationSettingsCard",
                        "StartupSettingsCard"
                    }
                )
                {
                    Assert.IsType<Border>(
                        window.FindName(
                            cardName
                        )
                    );
                }

                foreach (
                    var controlName in new[]
                    {
                        "ApiKeyPasswordBox",
                        "TestApiKeyButton",
                        "TextTranslationHotkeyBox",
                        "ScreenshotTranslationHotkeyBox",
                        "ScreenshotSourceLanguageComboBox",
                        "ScreenshotTargetLanguageComboBox",
                        "ScreenshotLanguageSwapButton",
                        "StartWithWindowsCheckBox",
                        "SaveButton"
                    }
                )
                {
                    Assert.NotNull(
                        window.FindName(
                            controlName
                        )
                    );
                }

                window.Close();
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
                    catch (
                        Exception caught
                    )
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
}
