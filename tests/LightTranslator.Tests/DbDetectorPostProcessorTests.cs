using LightTranslator.Models;
using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class DbDetectorPostProcessorTests
{
    [Fact]
    public void Process_RejectsWeakBoxesAndClipsToImage()
    {
        var map = new float[50, 100];

        FillRectangle(
            map,
            x: 20,
            y: 10,
            width: 60,
            height: 20,
            probability: 0.90f
        );

        FillRectangle(
            map,
            x: 2,
            y: 2,
            width: 8,
            height: 5,
            probability: 0.40f
        );

        var processor =
            new DbDetectorPostProcessor(
                pixelThreshold: 0.30f,
                boxThreshold: 0.60f,
                unclipRatio: 1.50f
            );

        var boxes =
            processor.Process(
                map,
                imageWidth: 100,
                imageHeight: 50
            );

        var box = Assert.Single(boxes);

        Assert.True(box.X >= 0);
        Assert.True(box.Y >= 0);
        Assert.True(box.X + box.Width <= 100);
        Assert.True(box.Y + box.Height <= 50);
        Assert.True(box.Width >= 60);
        Assert.True(box.Height >= 20);
    }

    [Fact]
    public void Process_ScalesProbabilityMapCoordinatesToOriginalImage()
    {
        var map = new float[25, 50];

        FillRectangle(
            map,
            x: 10,
            y: 5,
            width: 20,
            height: 10,
            probability: 0.95f
        );

        var processor =
            new DbDetectorPostProcessor(
                pixelThreshold: 0.30f,
                boxThreshold: 0.60f,
                unclipRatio: 1.00f
            );

        var box =
            Assert.Single(
                processor.Process(
                    map,
                    imageWidth: 200,
                    imageHeight: 100
                )
            );

        Assert.InRange(box.X, 38, 42);
        Assert.InRange(box.Y, 18, 22);
        Assert.InRange(box.Width, 78, 82);
        Assert.InRange(box.Height, 38, 42);
    }

    private static void FillRectangle(
        float[,] map,
        int x,
        int y,
        int width,
        int height,
        float probability
    )
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
            {
                map[row, column] = probability;
            }
        }
    }
}
