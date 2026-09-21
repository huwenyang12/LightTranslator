using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public readonly record struct ScreenshotBackgroundStyle(
    Color Background,
    Color Foreground
);

public static class ScreenshotBackgroundStyleResolver
{
    private const int SampleGridSize = 5;
    private const double MaximumLumaDeviation = 12d;
    private const int MaximumChannelRange = 24;
    private const double LightBackgroundThreshold = 0.55d;

    private static readonly Color LightForeground =
        Color.FromRgb(
            41,
            41,
            41
        );

    public static ScreenshotBackgroundStyle Fallback { get; } =
        new(
            Color.FromArgb(
                235,
                17,
                24,
                39
            ),
            Colors.White
        );

    public static ScreenshotBackgroundStyle Resolve(
        BitmapSource image,
        PixelRect bounds
    )
    {
        if (
            image is null ||
            !IsInsideImage(
                image,
                bounds
            ) ||
            !TryGetPixelFormat(
                image.Format,
                out var bytesPerPixel,
                out var isPremultiplied
            )
        )
        {
            return Fallback;
        }

        try
        {
            var samplePoints =
                CreatePerimeterSamplePoints(
                    image,
                    bounds
                );

            var samples =
                new List<RgbSample>(
                    samplePoints.Count
                );

            var pixel =
                new byte[
                    bytesPerPixel
                ];

            foreach (var point in samplePoints)
            {
                image.CopyPixels(
                    new Int32Rect(
                        point.X,
                        point.Y,
                        1,
                        1
                    ),
                    pixel,
                    bytesPerPixel,
                    0
                );

                samples.Add(
                    DecodePixel(
                        pixel,
                        isPremultiplied
                    )
                );
            }

            if (samples.Count == 0)
            {
                return Fallback;
            }

            var red =
                Median(
                    samples.Select(
                        sample =>
                            sample.Red
                    )
                );

            var green =
                Median(
                    samples.Select(
                        sample =>
                            sample.Green
                    )
                );

            var blue =
                Median(
                    samples.Select(
                        sample =>
                            sample.Blue
                    )
                );

            var lumas =
                samples
                    .Select(
                        sample =>
                            CalculateLuma(
                                sample.Red,
                                sample.Green,
                                sample.Blue
                            )
                    )
                    .ToArray();

            var meanLuma =
                lumas.Average();

            var lumaDeviation =
                Math.Sqrt(
                    lumas.Average(
                        luma =>
                            Math.Pow(
                                luma -
                                meanLuma,
                                2d
                            )
                    )
                );

            var isFlat =
                lumaDeviation <=
                MaximumLumaDeviation &&
                ChannelRange(
                    samples.Select(
                        sample =>
                            sample.Red
                    )
                ) <= MaximumChannelRange &&
                ChannelRange(
                    samples.Select(
                        sample =>
                            sample.Green
                    )
                ) <= MaximumChannelRange &&
                ChannelRange(
                    samples.Select(
                        sample =>
                            sample.Blue
                    )
                ) <= MaximumChannelRange;

            if (!isFlat)
            {
                return Fallback;
            }

            var normalizedMedianLuma =
                CalculateLuma(
                    red,
                    green,
                    blue
                ) /
                255d;

            return
                new ScreenshotBackgroundStyle(
                    Color.FromArgb(
                        255,
                        red,
                        green,
                        blue
                    ),
                    normalizedMedianLuma >=
                    LightBackgroundThreshold
                        ? LightForeground
                        : Colors.White
                );
        }
        catch (Exception)
        {
            return Fallback;
        }
    }

    private static bool IsInsideImage(
        BitmapSource image,
        PixelRect bounds
    )
    {
        if (
            bounds.IsEmpty ||
            bounds.X < 0 ||
            bounds.Y < 0
        )
        {
            return false;
        }

        var right =
            (long)bounds.X +
            bounds.Width;

        var bottom =
            (long)bounds.Y +
            bounds.Height;

        return
            right <= image.PixelWidth &&
            bottom <= image.PixelHeight;
    }

    private static bool TryGetPixelFormat(
        PixelFormat format,
        out int bytesPerPixel,
        out bool isPremultiplied
    )
    {
        isPremultiplied =
            false;

        if (format == PixelFormats.Bgr24)
        {
            bytesPerPixel =
                3;

            return true;
        }

        if (
            format == PixelFormats.Bgr32 ||
            format == PixelFormats.Bgra32 ||
            format == PixelFormats.Pbgra32
        )
        {
            bytesPerPixel =
                4;

            isPremultiplied =
                format ==
                PixelFormats.Pbgra32;

            return true;
        }

        bytesPerPixel =
            0;

        return false;
    }

    private static int[] CreateSamplePositions(
        int start,
        int length
    )
    {
        var first =
            start;

        var last =
            start +
            length -
            1;

        if (length > 2)
        {
            first++;
            last--;
        }

        return
            Enumerable
                .Range(
                    0,
                    SampleGridSize
                )
                .Select(
                    index =>
                        (
                            int
                        )Math.Round(
                            first +
                            (
                                last -
                                first
                            ) *
                            index /
                            (
                                SampleGridSize -
                                1d
                            ),
                            MidpointRounding.AwayFromZero
                        )
                )
                .Distinct()
                .ToArray();
    }

    private static IReadOnlyList<(int X, int Y)>
        CreatePerimeterSamplePoints(
            BitmapSource image,
            PixelRect bounds
        )
    {
        const int samplingOffset = 2;

        var left =
            Math.Max(
                0,
                bounds.X -
                samplingOffset
            );

        var right =
            Math.Min(
                image.PixelWidth -
                1,
                bounds.X +
                bounds.Width -
                1 +
                samplingOffset
            );

        var top =
            Math.Max(
                0,
                bounds.Y -
                samplingOffset
            );

        var bottom =
            Math.Min(
                image.PixelHeight -
                1,
                bounds.Y +
                bounds.Height -
                1 +
                samplingOffset
            );

        var xPositions =
            CreateSamplePositions(
                left,
                right -
                left +
                1
            );

        var yPositions =
            CreateSamplePositions(
                top,
                bottom -
                top +
                1
            );

        return
            xPositions
                .SelectMany(
                    x =>
                        new[]
                        {
                            (X: x, Y: top),
                            (X: x, Y: bottom)
                        }
                )
                .Concat(
                    yPositions.SelectMany(
                        y =>
                            new[]
                            {
                                (X: left, Y: y),
                                (X: right, Y: y)
                            }
                    )
                )
                .Distinct()
                .ToArray();
    }

    private static RgbSample DecodePixel(
        IReadOnlyList<byte> pixel,
        bool isPremultiplied
    )
    {
        var blue =
            pixel[0];

        var green =
            pixel[1];

        var red =
            pixel[2];

        if (
            !isPremultiplied ||
            pixel[3] == 0 ||
            pixel[3] == 255
        )
        {
            return
                new RgbSample(
                    red,
                    green,
                    blue
                );
        }

        var alpha =
            pixel[3];

        return
            new RgbSample(
                Unpremultiply(
                    red,
                    alpha
                ),
                Unpremultiply(
                    green,
                    alpha
                ),
                Unpremultiply(
                    blue,
                    alpha
                )
            );
    }

    private static byte Unpremultiply(
        byte value,
        byte alpha
    )
    {
        return
            (
                byte
            )Math.Min(
                255d,
                Math.Round(
                    value *
                    255d /
                    alpha,
                    MidpointRounding.AwayFromZero
                )
            );
    }

    private static byte Median(
        IEnumerable<byte> values
    )
    {
        var ordered =
            values
                .OrderBy(
                    value =>
                        value
                )
                .ToArray();

        var middle =
            ordered.Length /
            2;

        if (ordered.Length % 2 != 0)
        {
            return ordered[middle];
        }

        return
            (
                byte
            )Math.Round(
                (
                    ordered[middle - 1] +
                    ordered[middle]
                ) /
                2d,
                MidpointRounding.AwayFromZero
            );
    }

    private static int ChannelRange(
        IEnumerable<byte> values
    )
    {
        var samples =
            values.ToArray();

        return
            samples.Max() -
            samples.Min();
    }

    private static double CalculateLuma(
        byte red,
        byte green,
        byte blue
    )
    {
        return
            0.2126d *
            red +
            0.7152d *
            green +
            0.0722d *
            blue;
    }

    private readonly record struct RgbSample(
        byte Red,
        byte Green,
        byte Blue
    );
}
