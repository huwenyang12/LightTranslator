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
