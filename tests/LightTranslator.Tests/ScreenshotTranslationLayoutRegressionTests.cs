using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationLayoutRegressionTests
{
    [Fact]
    public void ShowResults_LongTranslationStaysInsideSourceBoxAndShrinksFont()
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
                                "Low Risk",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    120,
                                    30
                                ),
                                "供应商风险较低，请继续检查订单信息"
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

                    var translatedText =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    var mappedSourceWidth =
                        120d * 96d / 120d;

                    var mappedSourceHeight =
                        30d * 96d / 120d;

                    Assert.Equal(
                        mappedSourceWidth,
                        container.Width,
                        6
                    );

                    Assert.Equal(
                        mappedSourceHeight,
                        container.Height,
                        6
                    );

                    Assert.Equal(
                        TextWrapping.Wrap,
                        translatedText.TextWrapping
                    );

                    Assert.Equal(
                        TextTrimming.None,
                        translatedText.TextTrimming
                    );

                    Assert.InRange(
                        translatedText.FontSize,
                        6d,
                        17.999d
                    );
                }
                finally
                {
                    window.Close();
                }
            }
        );
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
                    0,
                    0,
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
