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
                        80,
                        Canvas.GetLeft(
                            translatedBlock
                        ),
                        6
                    );

                    Assert.Equal(
                        40,
                        Canvas.GetTop(
                            translatedBlock
                        ),
                        6
                    );

                    Assert.Equal(
                        160,
                        translatedBlock.Width,
                        6
                    );

                    Assert.Equal(
                        32,
                        translatedBlock.Height,
                        6
                    );

                    var translatedText =
                        Assert.IsType<TextBlock>(
                            translatedBlock.Child
                        );

                    Assert.Equal(
                        "译文",
                        translatedText.Text
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
