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
                        18d,
                        24d
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
                        166d
                    );

                    Assert.True(
                        Canvas.GetTop(
                            container
                        ) +
                        container.Height <=
                        180d
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
    public void ShowResults_LongParagraphUsesSourceRegionHeightInsteadOfStayingTiny()
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
                                "paragraph",
                                "Source paragraph",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    300,
                                    120
                                ),
                                15,
                                ScreenshotTextRole.Body,
                                new string(
                                    '译',
                                    50
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

                    Assert.InRange(
                        translatedText.FontSize,
                        14d,
                        20d
                    );

                    Assert.InRange(
                        translatedText.DesiredSize.Height,
                        69d,
                        75d
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
    public void ShowResults_AdaptiveBodyFontNeverExceedsMaximumReadableSize()
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
                                "body",
                                "Source",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    500,
                                    400
                                ),
                                50,
                                ScreenshotTextRole.Body,
                                "短正文"
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
                        38d,
                        translatedText.FontSize,
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
                        10d
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
    public void ShowResults_AlignsNearbyRegionsToOneTextColumn()
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
                                "body-1",
                                "First",
                                0.95,
                                new PixelRect(
                                    20,
                                    20,
                                    160,
                                    30
                                ),
                                30,
                                ScreenshotTextRole.Body,
                                "第一段"
                            ),
                            new ScreenshotTextRegion(
                                "body-2",
                                "Second",
                                0.95,
                                new PixelRect(
                                    30,
                                    100,
                                    160,
                                    30
                                ),
                                30,
                                ScreenshotTextRole.Body,
                                "第二段"
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

                    var firstTextLeft =
                        Canvas.GetLeft(
                            containers[0]
                        ) +
                        containers[0].Padding.Left;

                    var secondTextLeft =
                        Canvas.GetLeft(
                            containers[1]
                        ) +
                        containers[1].Padding.Left;

                    Assert.Equal(
                        firstTextLeft,
                        secondTextLeft,
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
    public void ShowResults_DoesNotAlignNarrowIndependentBlocksOrExpandTheirCoverage()
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
                                "narrow-1",
                                "A",
                                0.95,
                                new PixelRect(
                                    10,
                                    20,
                                    10,
                                    30
                                ),
                                30,
                                ScreenshotTextRole.Body,
                                "甲"
                            ),
                            new ScreenshotTextRegion(
                                "narrow-2",
                                "B",
                                0.95,
                                new PixelRect(
                                    40,
                                    100,
                                    10,
                                    30
                                ),
                                30,
                                ScreenshotTextRole.Body,
                                "乙"
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
                        6d,
                        Canvas.GetLeft(
                            containers[0]
                        ),
                        6
                    );

                    Assert.Equal(
                        30d,
                        Canvas.GetLeft(
                            containers[1]
                        ),
                        6
                    );

                    Assert.All(
                        containers,
                        container =>
                        {
                            Assert.Equal(
                                12d,
                                container.Width,
                                6
                            );

                            Assert.True(
                                container.Width -
                                container.Padding.Left -
                                container.Padding.Right >
                                0d
                            );
                        }
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
