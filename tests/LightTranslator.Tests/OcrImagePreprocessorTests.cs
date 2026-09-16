using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class OcrImagePreprocessorTests
{
    [Fact]
    public void PrepareDetector_UsesRgbNchwAndRoundsToMultipleOf32()
    {
        var image = CreateSolidBitmap(
            width: 1000,
            height: 500,
            red: 255,
            green: 0,
            blue: 0
        );

        var actual =
            new OcrImagePreprocessor()
                .PrepareDetector(image);

        Assert.Equal(
            new[] { 1, 3, 480, 960 },
            actual.Tensor.Dimensions.ToArray()
        );
        Assert.Equal(1000, actual.OriginalWidth);
        Assert.Equal(500, actual.OriginalHeight);
        Assert.Equal(960, actual.ResizedWidth);
        Assert.Equal(480, actual.ResizedHeight);

        Assert.Equal(
            (1f - 0.485f) / 0.229f,
            actual.Tensor[0, 0, 0, 0],
            5
        );
        Assert.Equal(
            (0f - 0.456f) / 0.224f,
            actual.Tensor[0, 1, 0, 0],
            5
        );
        Assert.Equal(
            (0f - 0.406f) / 0.225f,
            actual.Tensor[0, 2, 0, 0],
            5
        );
    }

    [Fact]
    public void PrepareRecognizer_ResizesToHeight48AndPadsToWidth320()
    {
        var image = CreateSolidBitmap(
            width: 200,
            height: 100,
            red: 255,
            green: 255,
            blue: 255
        );

        var actual =
            new OcrImagePreprocessor()
                .PrepareRecognizer(
                    image,
                    new PixelRect(
                        20,
                        10,
                        100,
                        50
                    )
                );

        Assert.Equal(
            new[] { 1, 3, 48, 320 },
            actual.Tensor.Dimensions.ToArray()
        );
        Assert.Equal(100, actual.OriginalWidth);
        Assert.Equal(50, actual.OriginalHeight);
        Assert.Equal(96, actual.ResizedWidth);
        Assert.Equal(48, actual.ResizedHeight);
        Assert.Equal(1f, actual.Tensor[0, 0, 0, 0], 5);
        Assert.Equal(0f, actual.Tensor[0, 0, 0, 319], 5);
    }

    [Fact]
    public void PrepareRecognizer_CapsVeryWideCropAt320()
    {
        var image = CreateSolidBitmap(
            width: 800,
            height: 50,
            red: 0,
            green: 0,
            blue: 0
        );

        var actual =
            new OcrImagePreprocessor()
                .PrepareRecognizer(
                    image,
                    new PixelRect(
                        0,
                        0,
                        800,
                        50
                    )
                );

        Assert.Equal(320, actual.ResizedWidth);
        Assert.Equal(48, actual.ResizedHeight);
        Assert.Equal(
            new[] { 1, 3, 48, 320 },
            actual.Tensor.Dimensions.ToArray()
        );
    }

    private static BitmapSource CreateSolidBitmap(
        int width,
        int height,
        byte red,
        byte green,
        byte blue
    )
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];

        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = blue;
            pixels[index + 1] = green;
            pixels[index + 2] = red;
            pixels[index + 3] = 255;
        }

        var bitmap =
            BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride
            );

        bitmap.Freeze();
        return bitmap;
    }
}
