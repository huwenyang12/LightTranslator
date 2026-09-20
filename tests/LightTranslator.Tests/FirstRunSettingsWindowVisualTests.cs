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
                    520d,
                    window.Width
                );
                Assert.Equal(
                    640d,
                    window.Height
                );
                Assert.Equal(520d, window.MinWidth);
                Assert.Equal(560d, window.MinHeight);
                Assert.Equal(WindowStyle.None, window.WindowStyle);

                var titleBar =
                    Assert.IsType<Grid>(
                        window.FindName("SettingsTitleBar")
                    );

                Assert.Equal(36d, titleBar.Height);

                var title =
                    Assert.IsType<TextBlock>(
                        window.FindName(
                            "TitleTextBlock"
                        )
                    );

                Assert.Equal(expectedTitle, title.Text);

                var header =
                    Assert.IsType<StackPanel>(title.Parent);

                Assert.Single(header.Children);
                Assert.Equal(new Thickness(20, 6, 20, 12), header.Margin);

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
                Assert.Equal(20d, appIcon.Width);
                Assert.Equal(20d, appIcon.Height);

                Assert.IsType<Button>(
                    window.FindName("SettingsCloseButton")
                );

                Assert.IsType<CheckBox>(
                    window.FindName(
                        "StartWithWindowsCheckBox"
                    )
                );

                var settingsScrollViewer =
                    Assert.IsType<ScrollViewer>(
                        window.FindName(
                            "SettingsScrollViewer"
                        )
                    );

                Assert.False(settingsScrollViewer.Focusable);

                var settingsCardsPanel =
                    Assert.IsType<StackPanel>(settingsScrollViewer.Content);

                Assert.Equal(
                    new Thickness(20, 0, 8, 16),
                    settingsCardsPanel.Margin
                );

                var scrollBarStyle =
                    Assert.IsType<Style>(
                        settingsScrollViewer.Resources[
                            typeof(System.Windows.Controls.Primitives.ScrollBar)
                        ]
                    );
                var widthSetter =
                    scrollBarStyle
                        .Setters
                        .OfType<Setter>()
                        .Single(
                            setter =>
                                setter.Property == FrameworkElement.WidthProperty
                        );
                var marginSetter =
                    scrollBarStyle
                        .Setters
                        .OfType<Setter>()
                        .Single(
                            setter =>
                                setter.Property == FrameworkElement.MarginProperty
                        );

                Assert.Equal(6d, widthSetter.Value);
                Assert.Equal(new Thickness(0, 8, 6, 8), marginSetter.Value);

                var scrollBar =
                    new System.Windows.Controls.Primitives.ScrollBar
                    {
                        Orientation = Orientation.Vertical,
                        Style = scrollBarStyle
                    };

                Assert.True(scrollBar.ApplyTemplate());

                var scrollTrack =
                    Assert.IsType<System.Windows.Controls.Primitives.Track>(
                        scrollBar.Template.FindName("PART_Track", scrollBar)
                    );

                Assert.True(double.IsNaN(scrollTrack.ViewportSize));
                Assert.Equal(52d, scrollTrack.Thumb.Height);

                var saveButton =
                    Assert.IsType<Button>(
                        window.FindName("SaveButton")
                    );

                Assert.Equal(72d, saveButton.Width);
                Assert.Equal(32d, saveButton.Height);
                Assert.Equal(13d, saveButton.FontSize);
                Assert.Equal(new Thickness(0), saveButton.BorderThickness);
                Assert.Same(
                    window.FindResource("Style.Settings.SaveButton"),
                    saveButton.Style
                );

                Assert.True(saveButton.ApplyTemplate());

                var saveButtonSurface =
                    Assert.IsType<Border>(
                        saveButton.Template.FindName("ButtonSurface", saveButton)
                    );

                Assert.Equal(new CornerRadius(10), saveButtonSurface.CornerRadius);

                var footer =
                    Assert.IsType<Border>(
                        VisualTreeHelper.GetParent(saveButton)
                    );

                Assert.Equal(new Thickness(20, 8, 20, 8), footer.Padding);
                Assert.Equal(new Thickness(0, 1, 0, 0), footer.BorderThickness);

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
                    var card =
                        Assert.IsType<Border>(
                            window.FindName(cardName)
                        );

                    Assert.Equal(new Thickness(14), card.Padding);
                    Assert.Equal(new CornerRadius(12), card.CornerRadius);
                }

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

    [Fact]
    public void CloseButton_ClosesSettingsWindow()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new FirstRunSettingsWindow(
                        new FirstRunSettingsViewModel(),
                        isFirstRun: false
                    );
                var closeButton =
                    Assert.IsType<Button>(
                        window.FindName("SettingsCloseButton")
                    );

                window.Show();
                closeButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent)
                );

                Assert.False(window.IsVisible);
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
