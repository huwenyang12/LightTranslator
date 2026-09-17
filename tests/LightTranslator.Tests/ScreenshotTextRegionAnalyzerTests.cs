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
}
