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
    public void ShowResults_LongTranslationExpandsIntoAvailableSpaceWithoutTinyText()
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

                    var mappedSourceWidthWithCoverage =
                        120d * 96d / 120d +
                        4d;

                    var mappedSourceHeight =
                        30d * 96d / 120d;

                    Assert.Equal(
                        mappedSourceWidthWithCoverage,
                        container.Width,
                        6
                    );

                    Assert.True(
                        container.Height >
                        mappedSourceHeight
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
                        12d,
                        16.8d
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
    public void ShowResults_ExtremeTranslationKeepsReadableMinimumAndUsesAvailableHeight()
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
                                "X",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    60,
                                    12
                                ),
                                new string(
                                    '译',
                                    200
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

                    var container =
                        Assert.Single(
                            canvas.Children
                                .OfType<Border>()
                        );

                    var translatedText =
                        Assert.IsType<TextBlock>(
                            container.Child
                        );

                    Assert.Equal(
                        12d,
                        translatedText.FontSize,
                        6
                    );

                    Assert.True(
                        container.ClipToBounds
                    );

                    Assert.Equal(
                        52d,
                        container.Width,
                        6
                    );

                    Assert.InRange(
                        container.Height,
                        100d,
                        184d
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
    public void ShowResults_ExpandedParagraphStopsBeforeFollowingRegion()
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
                                "Summary",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    120,
                                    20
                                ),
                                "这是一个需要自动换行并利用下方留白显示的较长翻译段落"
                            ),
                            new OcrBlock(
                                "block-0002",
                                "Next",
                                0.95,
                                new PixelRect(
                                    20,
                                    100,
                                    120,
                                    20
                                ),
                                "下一段"
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

                    var firstBottom =
                        Canvas.GetTop(
                            containers[0]
                        ) +
                        containers[0].Height;

                    var secondTop =
                        Canvas.GetTop(
                            containers[1]
                        );

                    Assert.True(
                        containers[0].Height >
                        16d
                    );

                    Assert.True(
                        firstBottom <=
                        secondTop -
                        4d
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
