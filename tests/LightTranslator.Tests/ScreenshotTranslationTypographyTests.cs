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
            15.624d,
            sizes["body-1"],
            6
        );

        Assert.Equal(
            15.624d,
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
            16.9344d,
            sizes["title"],
            6
        );

        Assert.Equal(
            15.12d,
            sizes["body"],
            6
        );

        Assert.True(
            sizes["title"] >
            sizes["body"]
        );
    }

    [Theory]
    [InlineData(96d, 24d, 15.12d)]
    [InlineData(120d, 30d, 15.12d)]
    [InlineData(144d, 36d, 15.12d)]
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

    [Fact]
    public void CalculatePreferredFontSizes_ClampsBodyOutliersAroundPageMedian()
    {
        var regions =
            new[]
            {
                CreateRegion(
                    "small",
                    "辅助文字",
                    20,
                    ScreenshotTextRole.Body
                ),
                CreateRegion(
                    "body",
                    "正文",
                    40,
                    ScreenshotTextRole.Body
                ),
                CreateRegion(
                    "large",
                    "异常放大的正文",
                    80,
                    ScreenshotTextRole.Body
                )
            };

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                96d
            );

        Assert.Equal(
            19.656d,
            sizes["small"],
            6
        );

        Assert.Equal(
            25.2d,
            sizes["body"],
            6
        );

        Assert.Equal(
            27.216d,
            sizes["large"],
            6
        );
    }

    [Fact]
    public void CalculatePreferredFontSizes_UsesCompactNumberedSectionLabels()
    {
        var regions =
            new[]
            {
                CreateRegion(
                    "section",
                    "第1段",
                    32,
                    ScreenshotTextRole.Title
                ),
                CreateRegion(
                    "body",
                    "正文",
                    40,
                    ScreenshotTextRole.Body
                )
            };

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                96d
            );

        Assert.Equal(
            20.664d,
            sizes["section"],
            6
        );

        Assert.True(
            sizes["section"] <
            sizes["body"]
        );
    }

    [Fact]
    public void CalculatePreferredFontSizes_KeepsOrdinaryTitleOnlyModestlyAboveBody()
    {
        var regions =
            new[]
            {
                CreateRegion(
                    "title",
                    "页面标题",
                    60,
                    ScreenshotTextRole.Title
                ),
                CreateRegion(
                    "body",
                    "正文",
                    40,
                    ScreenshotTextRole.Body
                )
            };

        var sizes =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                96d
            );

        Assert.Equal(
            28.224d,
            sizes["title"],
            6
        );

        Assert.Equal(
            25.2d,
            sizes["body"],
            6
        );
    }

    private static ScreenshotTextRegion CreateRegion(
        string id,
        string text,
        double sourceLineHeight,
        ScreenshotTextRole role
    )
    {
        return
            new ScreenshotTextRegion(
                id,
                text,
                0.95,
                new PixelRect(
                    0,
                    0,
                    300,
                    checked(
                        (int)sourceLineHeight
                    )
                ),
                sourceLineHeight,
                role
            );
    }
}
