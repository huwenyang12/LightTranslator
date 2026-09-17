using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotTextRegionAnalyzerTests
{
    [Fact]
    public void Analyze_GroupsBodyLinesInReadingOrderAndKeepsMedianLineHeight()
    {
        var blocks =
            new[]
            {
                new OcrBlock(
                    "line-3",
                    "third",
                    0.93,
                    new PixelRect(
                        20,
                        70,
                        220,
                        25
                    )
                ),
                new OcrBlock(
                    "line-1",
                    "first",
                    0.95,
                    new PixelRect(
                        20,
                        10,
                        200,
                        24
                    )
                ),
                new OcrBlock(
                    "line-2",
                    "second",
                    0.94,
                    new PixelRect(
                        20,
                        40,
                        240,
                        26
                    )
                )
            };

        var region =
            Assert.Single(
                ScreenshotTextRegionAnalyzer.Analyze(
                    blocks
                )
            );

        Assert.Equal(
            "region-0001",
            region.Id
        );

        Assert.Equal(
            "first second third",
            region.Text
        );

        Assert.Equal(
            ScreenshotTextRole.Body,
            region.Role
        );

        Assert.Equal(
            25d,
            region.SourceLineHeight,
            6
        );

        Assert.Equal(
            new PixelRect(
                20,
                10,
                240,
                85
            ),
            region.Bounds
        );

        Assert.Equal(
            0.94d,
            region.Confidence,
            6
        );
    }

    [Fact]
    public void Analyze_SeparatesLargerShortTitleFromFollowingBodyLines()
    {
        var regions =
            ScreenshotTextRegionAnalyzer.Analyze(
                new[]
                {
                    new OcrBlock(
                        "title",
                        "Paragraph 1",
                        0.98,
                        new PixelRect(
                            20,
                            10,
                            150,
                            40
                        )
                    ),
                    new OcrBlock(
                        "body-1",
                        "Learning a new language is rewarding.",
                        0.96,
                        new PixelRect(
                            20,
                            56,
                            420,
                            24
                        )
                    ),
                    new OcrBlock(
                        "body-2",
                        "It opens doors to new cultures.",
                        0.95,
                        new PixelRect(
                            20,
                            86,
                            440,
                            24
                        )
                    )
                }
            );

        Assert.Equal(
            2,
            regions.Count
        );

        Assert.Equal(
            ScreenshotTextRole.Title,
            regions[0].Role
        );

        Assert.Equal(
            "Paragraph 1",
            regions[0].Text
        );

        Assert.Equal(
            40d,
            regions[0].SourceLineHeight,
            6
        );

        Assert.Equal(
            ScreenshotTextRole.Body,
            regions[1].Role
        );

        Assert.Equal(
            "Learning a new language is rewarding. It opens doors to new cultures.",
            regions[1].Text
        );

        Assert.Equal(
            24d,
            regions[1].SourceLineHeight,
            6
        );
    }

    [Fact]
    public void Analyze_DoesNotClassifyIsolatedLargeLabelAsTitle()
    {
        var region =
            Assert.Single(
                ScreenshotTextRegionAnalyzer.Analyze(
                    new[]
                    {
                        new OcrBlock(
                            "label",
                            "VICTORY",
                            0.99,
                            new PixelRect(
                                20,
                                10,
                                200,
                                42
                            )
                        )
                    }
                )
            );

        Assert.Equal(
            ScreenshotTextRole.Body,
            region.Role
        );
    }

    [Fact]
    public void Analyze_DoesNotClassifySameHeightShortLineAsTitle()
    {
        var region =
            Assert.Single(
                ScreenshotTextRegionAnalyzer.Analyze(
                    new[]
                    {
                        new OcrBlock(
                            "line-1",
                            "Status",
                            0.98,
                            new PixelRect(
                                20,
                                10,
                                100,
                                24
                            )
                        ),
                        new OcrBlock(
                            "line-2",
                            "Connection is stable.",
                            0.96,
                            new PixelRect(
                                20,
                                40,
                                300,
                                24
                            )
                        ),
                        new OcrBlock(
                            "line-3",
                            "No action is required.",
                            0.95,
                            new PixelRect(
                                20,
                                70,
                                320,
                                24
                            )
                        )
                    }
                )
            );

        Assert.Equal(
            ScreenshotTextRole.Body,
            region.Role
        );
    }
}
