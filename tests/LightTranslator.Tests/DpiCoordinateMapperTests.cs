using System.Windows;
using LightTranslator.Models;
using LightTranslator.Services.ScreenCapture;

namespace LightTranslator.Tests;

public class DpiCoordinateMapperTests
{
    [Theory]
    [InlineData(96, 0, 0, 1000, 600, 0, 0, 1000, 600)]
    [InlineData(120, 100, 50, 1000, 600, 80, 40, 800, 480)]
    [InlineData(144, 150, 75, 900, 450, 100, 50, 600, 300)]
    public void PixelsToDips_UsesMonitorDpi(
        double dpi,
        int x,
        int y,
        int width,
        int height,
        double expectedX,
        double expectedY,
        double expectedWidth,
        double expectedHeight
    )
    {
        var actual =
            DpiCoordinateMapper.PixelsToDips(
                new PixelRect(
                    x,
                    y,
                    width,
                    height
                ),
                dpi,
                dpi
            );

        Assert.Equal(expectedX, actual.X, 6);
        Assert.Equal(expectedY, actual.Y, 6);
        Assert.Equal(expectedWidth, actual.Width, 6);
        Assert.Equal(expectedHeight, actual.Height, 6);
    }

    [Theory]
    [InlineData(96, 80, 40, 800, 480, 80, 40, 800, 480)]
    [InlineData(120, 80, 40, 800, 480, 100, 50, 1000, 600)]
    [InlineData(144, 100, 50, 600, 300, 150, 75, 900, 450)]
    public void DipsToPixels_UsesMonitorDpi(
        double dpi,
        double x,
        double y,
        double width,
        double height,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        var actual =
            DpiCoordinateMapper.DipsToPixels(
                new Rect(
                    x,
                    y,
                    width,
                    height
                ),
                dpi,
                dpi
            );

        Assert.Equal(
            new PixelRect(
                expectedX,
                expectedY,
                expectedWidth,
                expectedHeight
            ),
            actual
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PixelsToDips_WithInvalidDpi_Throws(double dpi)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                DpiCoordinateMapper.PixelsToDips(
                    new PixelRect(
                        0,
                        0,
                        100,
                        100
                    ),
                    dpi,
                    96
                )
        );
    }
}
