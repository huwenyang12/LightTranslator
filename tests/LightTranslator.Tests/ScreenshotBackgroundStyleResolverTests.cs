using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotBackgroundStyleResolverTests
{
    [Fact]
    public void Resolve_FlatLightRegionUsesSampledBackgroundAndBlackText()
    {
        var style =
            ScreenshotBackgroundStyleResolver.Resolve(
                CreateSolidBitmap(
                    100,
                    60,
                    245,
                    245,
                    245
                ),
                new PixelRect(
                    10,
                    10,
                    60,
                    30
                )
            );

        Assert.Equal(
            Color.FromArgb(
                255,
                245,
                245,
                245
            ),
            style.Background
        );

        Assert.Equal(
            Colors.Black,
            style.Foreground
        );
    }

    [Fact]
    public void Resolve_FlatDarkRegionUsesSampledBackgroundAndWhiteText()
    {
        var style =
            ScreenshotBackgroundStyleResolver.Resolve(
                CreateSolidBitmap(
                    100,
                    60,
                    20,
                    20,
                    20
                ),
                new PixelRect(
                    10,
                    10,
                    60,
                    30
                )
            );

        Assert.Equal(
            Color.FromArgb(
                255,
                20,
                20,
                20
            ),
            style.Background
        );

        Assert.Equal(
            Colors.White,
            style.Foreground
        );
    }

    [Fact]
    public void Resolve_LightDocumentIgnoresDarkGlyphsInsideTextBounds()
    {
        var style =
            ScreenshotBackgroundStyleResolver.Resolve(
                CreateLightDocumentBitmapWithDarkGlyphs(
                    100,
                    60
                ),
                new PixelRect(
                    10,
                    10,
                    60,
                    30
                )
            );

        Assert.Equal(
            Color.FromArgb(
                255,
                248,
                248,
                248
            ),
            style.Background
        );

        Assert.Equal(
            Colors.Black,
            style.Foreground
        );
    }

    [Fact]
    public void Resolve_HighVarianceRegionUsesDeterministicFallback()
    {
        var style =
            ScreenshotBackgroundStyleResolver.Resolve(
                CreateCheckerboardBitmap(
                    100,
                    60
                ),
                new PixelRect(
                    10,
                    10,
                    60,
                    30
                )
            );

        Assert.Equal(
            ScreenshotBackgroundStyleResolver.Fallback,
            style
        );

        Assert.Equal(
            Color.FromArgb(
                235,
                17,
                24,
                39
            ),
            style.Background
        );

        Assert.Equal(
            Colors.White,
            style.Foreground
        );
    }

    [Fact]
    public void Resolve_OutOfBoundsRegionUsesDeterministicFallback()
    {
        var style =
            ScreenshotBackgroundStyleResolver.Resolve(
                CreateSolidBitmap(
                    20,
                    20,
                    245,
                    245,
                    245
                ),
                new PixelRect(
                    30,
                    30,
                    10,
                    10
                )
            );

        Assert.Equal(
            ScreenshotBackgroundStyleResolver.Fallback,
            style
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
        var pixels =
            new byte[
                width *
                height *
                4
            ];

        for (
            var index = 0;
            index < pixels.Length;
            index += 4
        )
        {
            pixels[index] =
                blue;

            pixels[index + 1] =
                green;

            pixels[index + 2] =
                red;

            pixels[index + 3] =
                255;
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
                width * 4
            );

        bitmap.Freeze();

        return bitmap;
    }

    private static BitmapSource CreateCheckerboardBitmap(
        int width,
        int height
    )
    {
        var pixels =
            new byte[
                width *
                height *
                4
            ];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value =
                    (
                        (x / 5) +
                        (y / 5)
                    ) % 2 == 0
                        ? (byte)0
                        : (byte)255;

                var index =
                    (
                        y *
                        width +
                        x
                    ) * 4;

                pixels[index] =
                    value;

                pixels[index + 1] =
                    value;

                pixels[index + 2] =
                    value;

                pixels[index + 3] =
                    255;
            }
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
                width * 4
            );

        bitmap.Freeze();

        return bitmap;
    }

    private static BitmapSource CreateLightDocumentBitmapWithDarkGlyphs(
        int width,
        int height
    )
    {
        var pixels =
            new byte[
                width *
                height *
                4
            ];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var isGlyph =
                    x >= 18 &&
                    x <= 62 &&
                    y >= 16 &&
                    y <= 32 &&
                    (
                        x % 6 <= 2 ||
                        y % 7 <= 1
                    );

                var value =
                    isGlyph
                        ? (byte)18
                        : (byte)248;

                var index =
                    (
                        y *
                        width +
                        x
                    ) * 4;

                pixels[index] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
                pixels[index + 3] = 255;
            }
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
                width * 4
            );

        bitmap.Freeze();

        return bitmap;
    }
}
