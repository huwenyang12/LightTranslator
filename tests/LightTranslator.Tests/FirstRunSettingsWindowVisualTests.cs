using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
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
                    540d,
                    window.Width
                );
                Assert.Equal(
                    680d,
                    window.Height
                );
                Assert.Equal(540d, window.MinWidth);
                Assert.Equal(600d, window.MinHeight);

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

                var appIcon =
                    Assert.IsType<Image>(
                        window.FindName(
                            "SettingsAppIcon"
                        )
                    );

                var iconSource =
                    Assert.IsType<BitmapImage>(
                        appIcon.Source
                    );

                Assert.Equal(
                    BitmapCacheOption.OnLoad,
                    iconSource.CacheOption
                );
                Assert.True(iconSource.PixelWidth > 0);
                Assert.True(iconSource.PixelHeight > 0);

                Assert.IsType<CheckBox>(
                    window.FindName(
                        "StartWithWindowsCheckBox"
                    )
                );

                var sourceLanguage =
                    Assert.IsType<ComboBox>(
                        window.FindName(
                            "ScreenshotSourceLanguageComboBox"
                        )
                    );

                var selectedItemTemplate =
                    Assert.IsType<DataTemplate>(
                        sourceLanguage.ItemTemplate
                    );

                sourceLanguage.ItemsSource =
                    LanguageOption.SourceLanguages;
                sourceLanguage.SelectedItem =
                    LanguageOption.SourceLanguages[0];

                var selectedItemText =
                    Assert.IsType<TextBlock>(
                        selectedItemTemplate.LoadContent()
                    );

                Assert.Equal(
                    "DisplayName",
                    BindingOperations.GetBinding(
                        selectedItemText,
                        TextBlock.TextProperty
                    )?.Path.Path
                );

                var renderedSelection =
                    new ContentPresenter
                    {
                        Content = LanguageOption.SourceLanguages[0],
                        ContentTemplate = selectedItemTemplate
                    };

                renderedSelection.Measure(new Size(160, 36));
                renderedSelection.Arrange(new Rect(0, 0, 160, 36));
                renderedSelection.UpdateLayout();

                var renderedText =
                    Assert.IsType<TextBlock>(
                        VisualTreeHelper.GetChild(
                            renderedSelection,
                            0
                        )
                    );

                Assert.Equal("自动检测", renderedText.Text);

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
