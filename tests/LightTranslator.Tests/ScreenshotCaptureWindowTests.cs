using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public class ScreenshotCaptureWindowTests
{
    [Fact]
    public void FormatSelectionDimensions_UsesPixelDimensions()
    {
        Assert.Equal(
            "640 × 360",
            ScreenshotCaptureWindow.FormatSelectionDimensions(
                new PixelRect(
                    10,
                    20,
                    640,
                    360
                )
            )
        );
    }

    [Fact]
    public void Window_ExposesInstructionAndSelectionFeedback()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotCaptureWindow(
                        new ScreenCaptureFrame(
                            CreateBitmap(
                                1280,
                                720,
                                96
                            ),
                            new PixelRect(
                                0,
                                0,
                                1280,
                                720
                            ),
                            96,
                            96
                        )
                    );

                try
                {
                    Assert.IsType<Border>(
                        window.FindName(
                            "CaptureInstructionPill"
                        )
                    );

                    var selectionBorder =
                        Assert.IsType<Border>(
                            window.FindName(
                                "SelectionRectangle"
                            )
                        );

                    Assert.Equal(
                        new CornerRadius(
                            8
                        ),
                        selectionBorder.CornerRadius
                    );

                    var dimensionPill =
                        Assert.IsType<Border>(
                            window.FindName(
                                "SelectionDimensionPill"
                            )
                        );

                    Assert.Equal(
                        Visibility.Collapsed,
                        dimensionPill.Visibility
                    );

                    Assert.IsType<TextBlock>(
                        window.FindName(
                            "SelectionDimensionTextBlock"
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
    public void ResetSelectionFeedback_HidesSelectionAndDimensions()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new ScreenshotCaptureWindow(
                        new ScreenCaptureFrame(
                            CreateBitmap(
                                1280,
                                720,
                                96
                            ),
                            new PixelRect(
                                0,
                                0,
                                1280,
                                720
                            ),
                            96,
                            96
                        )
                    );

                try
                {
                    var selectionBorder =
                        Assert.IsType<Border>(
                            window.FindName(
                                "SelectionRectangle"
                            )
                        );

                    var dimensionPill =
                        Assert.IsType<Border>(
                            window.FindName(
                                "SelectionDimensionPill"
                            )
                        );

                    selectionBorder.Visibility =
                        Visibility.Visible;

                    dimensionPill.Visibility =
                        Visibility.Visible;

                    window.ResetSelectionFeedback();

                    Assert.Equal(
                        Visibility.Collapsed,
                        selectionBorder.Visibility
                    );
                    Assert.Equal(
                        Visibility.Collapsed,
                        dimensionPill.Visibility
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
    public void Window_UsesFrozenMonitorImageAndDpiAdjustedBounds()
    {
        RunOnSta(
            () =>
            {
                var frame =
                    new ScreenCaptureFrame(
                        CreateBitmap(
                            1600,
                            900,
                            120
                        ),
                        new PixelRect(
                            -1600,
                            0,
                            1600,
                            900
                        ),
                        120,
                        120
                    );

                var window =
                    new ScreenshotCaptureWindow(
                        frame
                    );

                try
                {
                    var image =
                        Assert.IsType<Image>(
                            window.FindName(
                                "FrozenScreenImage"
                            )
                        );

                    Assert.Same(frame.Image, image.Source);
                    Assert.Equal(WindowState.Normal, window.WindowState);
                    Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
                    Assert.True(window.Topmost);
                    Assert.False(window.ShowInTaskbar);
                    Assert.Equal(-1280, window.Left, 6);
                    Assert.Equal(0, window.Top, 6);
                    Assert.Equal(1280, window.Width, 6);
                    Assert.Equal(720, window.Height, 6);
                }
                finally
                {
                    window.Close();
                }
            }
        );
    }

    [Fact]
    public void ConvertSelectionToPixels_UsesCapturedMonitorDpi()
    {
        RunOnSta(
            () =>
            {
                var frame =
                    new ScreenCaptureFrame(
                        CreateBitmap(
                            1600,
                            900,
                            120
                        ),
                        new PixelRect(
                            0,
                            0,
                            1600,
                            900
                        ),
                        120,
                        120
                    );

                var window =
                    new ScreenshotCaptureWindow(
                        frame
                    );

                try
                {
                    var actual =
                        window.ConvertSelectionToPixels(
                            new Rect(
                                80,
                                40,
                                160,
                                64
                            )
                        );

                    Assert.Equal(
                        new PixelRect(
                            100,
                            50,
                            200,
                            80
                        ),
                        actual
                    );
                }
                finally
                {
                    window.Close();
                }
            }
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
