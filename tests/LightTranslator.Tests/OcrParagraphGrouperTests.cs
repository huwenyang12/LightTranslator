using LightTranslator.Models;
using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class OcrParagraphGrouperTests
{
    [Fact]
    public void Group_MergesNearbyLinesIntoOneParagraphAndSplitsLargeVerticalGap()
    {
        var blocks =
            new[]
            {
                new OcrBlock(
                    "block-0001",
                    "Learning a new language is rewarding.",
                    0.96,
                    new PixelRect(
                        20,
                        10,
                        420,
                        24
                    )
                ),
                new OcrBlock(
                    "block-0002",
                    "It opens doors to new cultures and friendships.",
                    0.94,
                    new PixelRect(
                        20,
                        40,
                        500,
                        24
                    )
                ),
                new OcrBlock(
                    "block-0003",
                    "Practice every day to improve steadily.",
                    0.92,
                    new PixelRect(
                        20,
                        110,
                        450,
                        24
                    )
                ),
                new OcrBlock(
                    "block-0004",
                    "Consistency matters more than intensity.",
                    0.93,
                    new PixelRect(
                        20,
                        140,
                        470,
                        24
                    )
                )
            };

        var paragraphs =
            OcrParagraphGrouper.Group(
                blocks
            );

        Assert.Equal(
            2,
            paragraphs.Count
        );

        Assert.Equal(
            "paragraph-0001",
            paragraphs[0].Id
        );

        Assert.Equal(
            "Learning a new language is rewarding. It opens doors to new cultures and friendships.",
            paragraphs[0].Text
        );

        Assert.Equal(
            new PixelRect(
                20,
                10,
                500,
                54
            ),
            paragraphs[0].Bounds
        );

        Assert.Equal(
            "paragraph-0002",
            paragraphs[1].Id
        );

        Assert.Equal(
            "Practice every day to improve steadily. Consistency matters more than intensity.",
            paragraphs[1].Text
        );

        Assert.Equal(
            new PixelRect(
                20,
                110,
                470,
                54
            ),
            paragraphs[1].Bounds
        );
    }

    [Fact]
    public void Group_UsesReadingOrderEvenWhenInputIsUnsorted()
    {
        var blocks =
            new[]
            {
                new OcrBlock(
                    "block-0002",
                    "second line",
                    0.90,
                    new PixelRect(
                        20,
                        40,
                        220,
                        24
                    )
                ),
                new OcrBlock(
                    "block-0001",
                    "first line",
                    0.95,
                    new PixelRect(
                        20,
                        10,
                        200,
                        24
                    )
                )
            };

        var paragraph =
            Assert.Single(
                OcrParagraphGrouper.Group(
                    blocks
                )
            );

        Assert.Equal(
            "first line second line",
            paragraph.Text
        );
    }
}
