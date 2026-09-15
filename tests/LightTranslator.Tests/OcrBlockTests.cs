using LightTranslator.Models;

namespace LightTranslator.Tests;

public class OcrBlockTests
{
    [Fact]
    public void FilterAndSort_RemovesLowConfidenceAndUsesReadingOrder()
    {
        var blocks =
            new[]
            {
                new OcrBlock(
                    "b",
                    "right",
                    0.90,
                    new PixelRect(
                        200,
                        10,
                        80,
                        20
                    )
                ),
                new OcrBlock(
                    "low",
                    "secret",
                    0.49,
                    new PixelRect(
                        0,
                        0,
                        10,
                        10
                    )
                ),
                new OcrBlock(
                    "a",
                    "left",
                    0.80,
                    new PixelRect(
                        10,
                        12,
                        80,
                        20
                    )
                ),
                new OcrBlock(
                    "blank",
                    "   ",
                    0.99,
                    new PixelRect(
                        0,
                        40,
                        10,
                        10
                    )
                ),
                new OcrBlock(
                    "c",
                    "next",
                    0.70,
                    new PixelRect(
                        10,
                        80,
                        80,
                        20
                    )
                )
            };

        var actual =
            OcrBlock.FilterAndSort(
                blocks,
                0.50
            );

        Assert.Equal(
            new[]
            {
                "a",
                "b",
                "c"
            },
            actual.Select(block => block.Id)
        );
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-1, 10)]
    public void PixelRect_IsEmpty_WhenWidthOrHeightIsNotPositive(
        int width,
        int height
    )
    {
        var rectangle =
            new PixelRect(
                0,
                0,
                width,
                height
            );

        Assert.True(
            rectangle.IsEmpty
        );
    }

    [Fact]
    public void OcrBlock_WithTranslation_PreservesSourceData()
    {
        var source =
            new OcrBlock(
                "block-0001",
                "Hello",
                0.98,
                new PixelRect(
                    10,
                    20,
                    80,
                    30
                )
            );

        var translated =
            source with
            {
                TranslatedText = "你好"
            };

        Assert.Null(source.TranslatedText);
        Assert.Equal("你好", translated.TranslatedText);
        Assert.Equal(source.Id, translated.Id);
        Assert.Equal(source.Text, translated.Text);
        Assert.Equal(source.Bounds, translated.Bounds);
    }
}
