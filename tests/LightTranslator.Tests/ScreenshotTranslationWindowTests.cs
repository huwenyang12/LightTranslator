using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Screenshot;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationWindowTests
{
    [Fact]
    public void Window_UsesFrozenSelectionAndDpiAdjustedScreenBounds()
    {
        RunOnSta(
            () =>
            {
                var selection =
                    CreateSelection();

                var window =
                    new ScreenshotTranslationWindow(
                        selection
                    );

                try
                {
                    var image =
                        Assert.IsType<Image>(
                            window.FindName(
                                "FrozenSelectionImage"
                            )
                        );

                    Assert.IsAssignableFrom<IScreenshotResultView>(
                        window
                    );

                    Assert.Same(
                        selection.Image,
                        image.Source
                    );

                    Assert.True(
                        window.Topmost
                    );

                    Assert.False(
                        window.ShowInTaskbar
                    );

                    Assert.Equal(
                        WindowStartupLocation.Manual,
                        window.WindowStartupLocation
                    );

                    Assert.Equal(
                        -400,
                        window.Left,
                        6
                    );

                    Assert.Equal(
                        80,
                        window.Top,
                        6
                    );

                    Assert.Equal(
                        400,
                        window.Width,
                        6
                    );

                    Assert.Equal(
                        200,
                        window.Height,
                        6
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowLoading_KeepsFrozenImageAndDisplaysMessage()
    {
        RunOnSta(
            () =>
            {
                var selection =
                    CreateSelection();

                var window =
                    new ScreenshotTranslationWindow(
                        selection
                    );

                try
                {
                    window.ShowLoading(
                        selection,
                        "正在识别…"
                    );

                    var image =
                        Assert.IsType<Image>(
                            window.FindName(
                                "FrozenSelectionImage"
                            )
                        );

                    var status =
                        Assert.IsType<TextBlock>(
                            window.FindName(
                                "StatusTextBlock"
                            )
                        );

                    var progress =
                        Assert.IsType<ProgressBar>(
                            window.FindName(
                                "StatusProgressBar"
                            )
                        );

                    Assert.Same(
                        selection.Image,
                        image.Source
                    );

                    Assert.Equal(
                        "正在识别…",
                        status.Text
                    );

                    Assert.Equal(
                        Visibility.Visible,
                        status.Visibility
                    );

                    Assert.Equal(
                        Visibility.Visible,
                        progress.Visibility
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_RendersOnlyTranslatedTextAtMappedBounds()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new OcrBlock(
                                "block-0001",
                                "Secret source",
                                0.90,
                                new PixelRect(
                                    100,
                                    50,
                                    200,
                                    40
                                ),
                                "译文"
                            ),
                            new OcrBlock(
                                "block-0002",
                                "No translation",
                                0.80,
                                new PixelRect(
                                    10,
                                    10,
                                    50,
                                    20
                                )
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var translatedBlock =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    Assert.Equal(
                        78,
                        Canvas.GetLeft(
                            translatedBlock
                        ),
                        6
                    );

                    Assert.Equal(
                        38,
                        Canvas.GetTop(
                            translatedBlock
                        ),
                        6
                    );

                    Assert.Equal(
                        164,
                        translatedBlock.Width,
                        6
                    );

                    Assert.True(
                        translatedBlock.Height >=
                        36d
                    );

                    var translatedText =
                        Assert.IsType<TextBlock>(
                            translatedBlock.Child
                        );

                    Assert.Equal(
                        "译文",
                        translatedText.Text
                    );

                    Assert.Equal(
                        TextTrimming.None,
                        translatedText.TextTrimming
                    );

                    Assert.DoesNotContain(
                        canvas.Children
                            .OfType<Border>()
                            .Select(
                                child =>
                                    Assert.IsType<TextBlock>(
                                        child.Child
                                    ).Text
                            ),
                        text =>
                            text.Contains(
                                "Secret source",
                                StringComparison.Ordinal
                            ) ||
                            text.Contains(
                                "No translation",
                                StringComparison.Ordinal
                            )
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_UsesDynamicPaddingForSmallAndLargeRegions()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "small",
                                "Small",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    120,
                                    15
                                ),
                                15,
                                ScreenshotTextRole.Body,
                                "小"
                            ),
                            new ScreenshotTextRegion(
                                "large",
                                "Large",
                                0.95,
                                new PixelRect(
                                    20,
                                    60,
                                    240,
                                    50
                                ),
                                50,
                                ScreenshotTextRole.Body,
                                "大区域"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var containers =
                        canvas.Children
                            .OfType<Border>()
                            .ToArray();

                    Assert.Equal(
                        2,
                        containers.Length
                    );

                    Assert.Equal(
                        new Thickness(
                            4,
                            2,
                            4,
                            2
                        ),
                        containers[0].Padding
                    );

                    Assert.Equal(
                        new Thickness(
                            6,
                            3.2,
                            6,
                            3.2
                        ),
                        containers[1].Padding
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_TopAlignsExplicitMultilineTranslation()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "single",
                                "Single",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    300,
                                    50
                                ),
                                24,
                                ScreenshotTextRole.Body,
                                "Single line"
                            ),
                            new ScreenshotTextRegion(
                                "multiline",
                                "Multiline",
                                0.95,
                                new PixelRect(
                                    20,
                                    80,
                                    300,
                                    60
                                ),
                                24,
                                ScreenshotTextRole.Body,
                                "First line\nSecond line"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var texts =
                        canvas.Children
                            .OfType<Border>()
                            .Select(
                                container =>
                                    Assert.IsType<TextBlock>(
                                        container.Child
                                    )
                            )
                            .ToArray();

                    Assert.Equal(
                        VerticalAlignment.Center,
                        texts[0].VerticalAlignment
                    );

                    Assert.Equal(
                        VerticalAlignment.Top,
                        texts[1].VerticalAlignment
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_TopAlignsAutomaticallyWrappedTranslation()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "wrapped",
                                "Wrapped",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    120,
                                    30
                                ),
                                30,
                                ScreenshotTextRole.Body,
                                "这是一段需要自动换行并从顶部开始排列的翻译内容"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var container =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    var text =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    Assert.Equal(
                        VerticalAlignment.Top,
                        text.VerticalAlignment
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_KeepsLargeTitleAboveEighteenDip()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "title",
                                "Title",
                                0.98,
                                new PixelRect(
                                    20,
                                    20,
                                    500,
                                    100
                                ),
                                50,
                                ScreenshotTextRole.Title,
                                "Title"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var container =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    var title =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    Assert.Equal(
                        28d,
                        title.FontSize,
                        6
                    );

                    Assert.Equal(
                        FontWeights.SemiBold,
                        title.FontWeight
                    );

                    Assert.Equal(
                        38.64d,
                        title.LineHeight,
                        6
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_UsesSampledLightBackgroundAndDarkText()
    {
        RunOnSta(
            () =>
            {
                var selection =
                    CreateSelection(
                        CreateSolidBitmap(
                            100,
                            60,
                            245,
                            245,
                            245
                        )
                    );

                var window =
                    new ScreenshotTranslationWindow(
                        selection
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "light",
                                "Source",
                                0.95,
                                new PixelRect(
                                    10,
                                    10,
                                    60,
                                    30
                                ),
                                20,
                                ScreenshotTextRole.Body,
                                "译文"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var container =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    var text =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    var background =
                        Assert.IsType<SolidColorBrush>(
                            container.Background
                        );

                    var foreground =
                        Assert.IsType<SolidColorBrush>(
                            text.Foreground
                        );

                    Assert.Equal(
                        Color.FromArgb(
                            255,
                            245,
                            245,
                            245
                        ),
                        background.Color
                    );

                    Assert.Equal(
                        Colors.Black,
                        foreground.Color
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowResults_UsesFallbackStyleForComplexBackground()
    {
        RunOnSta(
            () =>
            {
                var selection =
                    CreateSelection(
                        CreateCheckerboardBitmap(
                            100,
                            60
                        )
                    );

                var window =
                    new ScreenshotTranslationWindow(
                        selection
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new ScreenshotTextRegion(
                                "complex",
                                "Source",
                                0.95,
                                new PixelRect(
                                    10,
                                    10,
                                    60,
                                    30
                                ),
                                20,
                                ScreenshotTextRole.Body,
                                "译文"
                            )
                        }
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var container =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    var text =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    var background =
                        Assert.IsType<SolidColorBrush>(
                            container.Background
                        );

                    var foreground =
                        Assert.IsType<SolidColorBrush>(
                            text.Foreground
                        );

                    Assert.Equal(
                        Color.FromArgb(
                            235,
                            17,
                            24,
                            39
                        ),
                        background.Color
                    );

                    Assert.Equal(
                        Colors.White,
                        foreground.Color
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ShowMessage_ReplacesLoadingStatusAndClearsTranslations()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    window.ShowResults(
                        new[]
                        {
                            new OcrBlock(
                                "block-0001",
                                "source",
                                0.90,
                                new PixelRect(
                                    10,
                                    10,
                                    100,
                                    30
                                ),
                                "译文"
                            )
                        }
                    );

                    window.ShowMessage(
                        "未识别到文字"
                    );

                    var canvas =
                        Assert.IsType<Canvas>(
                            window.FindName(
                                "TranslationCanvas"
                            )
                        );

                    var status =
                        Assert.IsType<TextBlock>(
                            window.FindName(
                                "StatusTextBlock"
                            )
                        );

                    var progress =
                        Assert.IsType<ProgressBar>(
                            window.FindName(
                                "StatusProgressBar"
                            )
                        );

                    Assert.Empty(
                        canvas.Children
                    );

                    Assert.Equal(
                        "未识别到文字",
                        status.Text
                    );

                    Assert.Equal(
                        Visibility.Visible,
                        status.Visibility
                    );

                    Assert.Equal(
                        Visibility.Collapsed,
                        progress.Visibility
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Theory]
    [InlineData("Mouse")]
    [InlineData("Escape")]
    [InlineData("AltQ")]
    public void CloseBindings_RaiseOneCloseRequest(
        string gesture
    )
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotTranslationWindow(
                        CreateSelection()
                    );

                try
                {
                    var count =
                        0;

                    window.CloseRequested +=
                        (_, _) =>
                            count++;

                    var binding =
                        FindCloseBinding(
                            window,
                            gesture
                        );

                    Assert.True(
                        binding.Command.CanExecute(
                            binding.CommandParameter
                        )
                    );

                    binding.Command.Execute(
                        binding.CommandParameter
                    );

                    binding.Command.Execute(
                        binding.CommandParameter
                    );

                    Assert.Equal(
                        1,
                        count
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    private static InputBinding FindCloseBinding(
        Window window,
        string gesture
    )
    {
        return
            gesture switch
            {
                "Mouse" =>
                    Assert.Single(
                        window.InputBindings
                            .OfType<MouseBinding>()
                            .Where(
                                binding =>
                                    binding.Gesture is MouseGesture mouse &&
                                    mouse.MouseAction == MouseAction.LeftClick
                            )
                    ),

                "Escape" =>
                    Assert.Single(
                        window.InputBindings
                            .OfType<KeyBinding>()
                            .Where(
                                binding =>
                                    binding.Key == Key.Escape &&
                                    binding.Modifiers == ModifierKeys.None
                            )
                    ),

                "AltQ" =>
                    Assert.Single(
                        window.InputBindings
                            .OfType<KeyBinding>()
                            .Where(
                                binding =>
                                    binding.Key == Key.Q &&
                                    binding.Modifiers == ModifierKeys.Alt
                            )
                    ),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(
                            gesture
                        )
                    )
            };
    }

    private static CapturedSelection CreateSelection(
        BitmapSource image
    )
    {
        return
            new CapturedSelection(
                image,
                new PixelRect(
                    0,
                    0,
                    image.PixelWidth,
                    image.PixelHeight
                ),
                new PixelRect(
                    0,
                    0,
                    image.PixelWidth,
                    image.PixelHeight
                ),
                96,
                96
            );
    }

    private static BitmapSource CreateSolidBitmap(
        int width,
        int height,
        byte red,
        byte green,
        byte blue
    )
    {
        var pixels =
            new byte[
                width *
                height *
                4
            ];

        for (
            var index = 0;
            index < pixels.Length;
            index += 4
        )
        {
            pixels[index] =
                blue;

            pixels[index + 1] =
                green;

            pixels[index + 2] =
                red;

            pixels[index + 3] =
                255;
        }

        var bitmap =
            BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                width * 4
            );

        bitmap.Freeze();

        return bitmap;
    }

    private static BitmapSource CreateCheckerboardBitmap(
        int width,
        int height
    )
    {
        var pixels =
            new byte[
                width *
                height *
                4
            ];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value =
                    (
                        (x / 5) +
                        (y / 5)
                    ) % 2 == 0
                        ? (byte)0
                        : (byte)255;

                var index =
                    (
                        y *
                        width +
                        x
                    ) * 4;

                pixels[index] =
                    value;

                pixels[index + 1] =
                    value;

                pixels[index + 2] =
                    value;

                pixels[index + 3] =
                    255;
            }
        }

        var bitmap =
            BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                width * 4
            );

        bitmap.Freeze();

        return bitmap;
    }

    private static CapturedSelection CreateSelection()
    {
        return
            new CapturedSelection(
                CreateBitmap(
                    500,
                    250,
                    120
                ),
                new PixelRect(
                    0,
                    0,
                    500,
                    250
                ),
                new PixelRect(
                    -500,
                    100,
                    500,
                    250
                ),
                120,
                120
            );
    }

    private static BitmapSource CreateBitmap(
        int width,
        int height,
        double dpi
    )
    {
        var bitmap =
            new WriteableBitmap(
                width,
                height,
                dpi,
                dpi,
                PixelFormats.Bgra32,
                null
            );

        bitmap.Freeze();

        return bitmap;
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
}
