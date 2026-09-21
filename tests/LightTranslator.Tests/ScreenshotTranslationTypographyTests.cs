using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationTypographyTests
{
    [Fact]
    public void CalculatePreferredFontSizes_NormalizesSimilarBodyRegions()
    {
        var regions =
            new[]
            {
                new ScreenshotTextRegion(
                    "body-1",
                    "Body one",
                    0.95,
                    new PixelRect(
                        0,
                        0,
                        300,
                        30
                    ),
                    30,
                    ScreenshotTextRole.Body
                ),
                new ScreenshotTextRegion(
                    "body-2",
                    "Body two",
                    0.95,
                    new PixelRect(
                        0,
                        40,
                        300,
                        32
                    ),
                    32,
                    ScreenshotTextRole.Body
                )
            };

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                120d
            );

        Assert.Equal(
            17.36d,
            sizes["body-1"],
            6
        );

        Assert.Equal(
            17.36d,
            sizes["body-2"],
            6
        );
    }

    [Fact]
    public void CalculatePreferredFontSizes_KeepsLargeTitleAboveBodySize()
    {
        var regions =
            new[]
            {
                new ScreenshotTextRegion(
                    "title",
                    "Title",
                    0.98,
                    new PixelRect(
                        0,
                        0,
                        200,
                        50
                    ),
                    50,
                    ScreenshotTextRole.Title
                ),
                new ScreenshotTextRegion(
                    "body",
                    "Body",
                    0.95,
                    new PixelRect(
                        0,
                        60,
                        300,
                        30
                    ),
                    30,
                    ScreenshotTextRole.Body
                )
            };

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                120d
            );

        Assert.Equal(
            28d,
            sizes["title"],
            6
        );

        Assert.Equal(
            16.8d,
            sizes["body"],
            6
        );

        Assert.True(
            sizes["title"] >
            sizes["body"]
        );
    }

    [Theory]
    [InlineData(96d, 24d, 16.8d)]
    [InlineData(120d, 30d, 16.8d)]
    [InlineData(144d, 36d, 16.8d)]
    public void CalculatePreferredFontSizes_ConvertsPhysicalLineHeightToDip(
        double dpiY,
        double sourceLineHeight,
        double expected
    )
    {
        var region =
            new ScreenshotTextRegion(
                "body",
                "Body",
                0.95,
                new PixelRect(
                    0,
                    0,
                    300,
                    60
                ),
                sourceLineHeight,
                ScreenshotTextRole.Body
            );

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                new[]
                {
                    region
                },
                dpiY
            );

        Assert.Equal(
            expected,
            sizes["body"],
            6
        );
    }
}
