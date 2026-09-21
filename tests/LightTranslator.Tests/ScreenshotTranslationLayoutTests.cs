using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationLayoutTests
{
    [Fact]
    public void CalculateAlignedLeftEdges_AlignsNearbyEdgesAndKeepsDistantColumn()
    {
        var regions =
            new[]
            {
                CreateRegion(
                    "left-1",
                    20
                ),
                CreateRegion(
                    "left-2",
                    30
                ),
                CreateRegion(
                    "right",
                    180
                )
            };

        var aligned =
            ScreenshotTranslationLayout.CalculateAlignedLeftEdges(
                regions,
                120d
            );

        Assert.Equal(
            20d,
            aligned["left-1"],
            6
        );

        Assert.Equal(
            aligned["left-1"],
            aligned["left-2"],
            6
        );

        Assert.Equal(
            144d,
            aligned["right"],
            6
        );
    }

    [Theory]
    [InlineData(ScreenshotTextRole.Title, ScreenshotTextRole.Body, 20d, 10d)]
    [InlineData(ScreenshotTextRole.Body, ScreenshotTextRole.Title, 20d, 18d)]
    [InlineData(ScreenshotTextRole.Body, ScreenshotTextRole.Body, 20d, 14.4d)]
    public void CalculateParagraphGap_UsesAdjacentTextRoles(
        ScreenshotTextRole currentRole,
        ScreenshotTextRole nextRole,
        double fontSize,
        double expected
    )
    {
        Assert.Equal(
            expected,
            ScreenshotTranslationLayout.CalculateParagraphGap(
                currentRole,
                nextRole,
                fontSize
            ),
            6
        );
    }

    [Theory]
    [InlineData(400d, 300d, 380d)]
    [InlineData(400d, 390d, 390d)]
    public void CalculateBottomLimit_ReservesSpaceWithoutExposingSource(
        double windowHeight,
        double sourceBottom,
        double expected
    )
    {
        Assert.Equal(
            expected,
            ScreenshotTranslationLayout.CalculateBottomLimit(
                windowHeight,
                sourceBottom
            ),
            6
        );
    }

    private static ScreenshotTextRegion CreateRegion(
        string id,
        int x
    )
    {
        return
            new ScreenshotTextRegion(
                id,
                id,
                0.95,
                new PixelRect(
                    x,
                    0,
                    100,
                    30
                ),
                30,
                ScreenshotTextRole.Body,
                id
            );
    }
}
