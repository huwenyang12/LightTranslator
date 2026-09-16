using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace LightTranslator.Services.Ocr;

internal sealed record OcrTensorInput(
    DenseTensor<float> Tensor,
    int OriginalWidth,
    int OriginalHeight,
    int ResizedWidth,
    int ResizedHeight
);

internal sealed class OcrImagePreprocessor
{
    private const int DetectorMaximumSide = 960;
    private const int RecognitionHeight = 48;
    private const int RecognitionWidth = 320;

    private static readonly float[] DetectorMean =
    {
        0.485f,
        0.456f,
        0.406f
    };

    private static readonly float[] DetectorStandardDeviation =
    {
        0.229f,
        0.224f,
        0.225f
    };

    public OcrTensorInput PrepareDetector(
        BitmapSource image
    )
    {
        ArgumentNullException.ThrowIfNull(
            image
        );

        var source =
            PixelBuffer.From(
                image
            );

        var scale =
            Math.Min(
                1d,
                DetectorMaximumSide /
                (double)Math.Max(
                    source.Width,
                    source.Height
                )
            );

        var resizedWidth =
            RoundToMultipleOf32(
                source.Width * scale
            );

        var resizedHeight =
            RoundToMultipleOf32(
                source.Height * scale
            );

        var tensor =
            new DenseTensor<float>(
                new[]
                {
                    1,
                    3,
                    resizedHeight,
                    resizedWidth
                }
            );

        FillTensor(
            source,
            sourceX: 0,
            sourceY: 0,
            sourceWidth: source.Width,
            sourceHeight: source.Height,
            destinationWidth: resizedWidth,
            destinationHeight: resizedHeight,
            writePixel:
                (channel, y, x, value) =>
                    tensor[
                        0,
                        channel,
                        y,
                        x
                    ] =
                        (
                            value / 255f -
                            DetectorMean[channel]
                        ) /
                        DetectorStandardDeviation[channel]
        );

        return
            new OcrTensorInput(
                tensor,
                source.Width,
                source.Height,
                resizedWidth,
                resizedHeight
            );
    }

    public OcrTensorInput PrepareRecognizer(
        BitmapSource image,
        PixelRect bounds
    )
    {
        ArgumentNullException.ThrowIfNull(
            image
        );

        var source =
            PixelBuffer.From(
                image
            );

        ValidateBounds(
            bounds,
            source.Width,
            source.Height
        );

        var resizedWidth =
            Math.Clamp(
                (int)Math.Round(
                    bounds.Width *
                    RecognitionHeight /
                    (double)bounds.Height,
                    MidpointRounding.AwayFromZero
                ),
                1,
                RecognitionWidth
            );

        var tensor =
            new DenseTensor<float>(
                new[]
                {
                    1,
                    3,
                    RecognitionHeight,
                    RecognitionWidth
                }
            );

        FillTensor(
            source,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            resizedWidth,
            RecognitionHeight,
            writePixel:
                (channel, y, x, value) =>
                    tensor[
                        0,
                        channel,
                        y,
                        x
                    ] =
                        (
                            value / 255f -
                            0.5f
                        ) /
                        0.5f
        );

        return
            new OcrTensorInput(
                tensor,
                bounds.Width,
                bounds.Height,
                resizedWidth,
                RecognitionHeight
            );
    }

    private static int RoundToMultipleOf32(
        double value
    )
    {
        return
            Math.Clamp(
                (int)Math.Round(
                    value / 32d,
                    MidpointRounding.AwayFromZero
                ) * 32,
                32,
                DetectorMaximumSide
            );
    }

    private static void ValidateBounds(
        PixelRect bounds,
        int imageWidth,
        int imageHeight
    )
    {
        if (bounds.IsEmpty ||
            bounds.X < 0 ||
            bounds.Y < 0 ||
            bounds.X > imageWidth - bounds.Width ||
            bounds.Y > imageHeight - bounds.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds)
            );
        }
    }

    private static void FillTensor(
        PixelBuffer source,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        int destinationWidth,
        int destinationHeight,
        Action<int, int, int, float> writePixel
    )
    {
        for (var y = 0;
             y < destinationHeight;
             y++)
        {
            var sampleY =
                sourceY +
                (
                    y + 0.5d
                ) *
                sourceHeight /
                destinationHeight -
                0.5d;

            for (var x = 0;
                 x < destinationWidth;
                 x++)
            {
                var sampleX =
                    sourceX +
                    (
                        x + 0.5d
                    ) *
                    sourceWidth /
                    destinationWidth -
                    0.5d;

                writePixel(
                    0,
                    y,
                    x,
                    source.Sample(
                        sampleX,
                        sampleY,
                        channelOffset: 2
                    )
                );

                writePixel(
                    1,
                    y,
                    x,
                    source.Sample(
                        sampleX,
                        sampleY,
                        channelOffset: 1
                    )
                );

                writePixel(
                    2,
                    y,
                    x,
                    source.Sample(
                        sampleX,
                        sampleY,
                        channelOffset: 0
                    )
                );
            }
        }
    }

    private sealed class PixelBuffer
    {
        private readonly byte[] _pixels;
        private readonly int _stride;

        private PixelBuffer(
            byte[] pixels,
            int stride,
            int width,
            int height
        )
        {
            _pixels = pixels;
            _stride = stride;
            Width = width;
            Height = height;
        }

        public int Width
        {
            get;
        }

        public int Height
        {
            get;
        }

        public static PixelBuffer From(
            BitmapSource image
        )
        {
            BitmapSource converted =
                image.Format == PixelFormats.Bgra32
                    ? image
                    : new FormatConvertedBitmap(
                        image,
                        PixelFormats.Bgra32,
                        null,
                        0
                    );

            var stride =
                converted.PixelWidth * 4;

            var pixels =
                new byte[
                    stride *
                    converted.PixelHeight
                ];

            converted.CopyPixels(
                pixels,
                stride,
                0
            );

            return
                new PixelBuffer(
                    pixels,
                    stride,
                    converted.PixelWidth,
                    converted.PixelHeight
                );
        }

        public float Sample(
            double x,
            double y,
            int channelOffset
        )
        {
            var boundedX =
                Math.Clamp(
                    x,
                    0d,
                    Width - 1d
                );

            var boundedY =
                Math.Clamp(
                    y,
                    0d,
                    Height - 1d
                );

            var x0 =
                (int)Math.Floor(
                    boundedX
                );

            var y0 =
                (int)Math.Floor(
                    boundedY
                );

            var x1 =
                Math.Min(
                    x0 + 1,
                    Width - 1
                );

            var y1 =
                Math.Min(
                    y0 + 1,
                    Height - 1
                );

            var xWeight =
                boundedX - x0;

            var yWeight =
                boundedY - y0;

            var top =
                Read(
                    x0,
                    y0,
                    channelOffset
                ) *
                (
                    1d - xWeight
                ) +
                Read(
                    x1,
                    y0,
                    channelOffset
                ) *
                xWeight;

            var bottom =
                Read(
                    x0,
                    y1,
                    channelOffset
                ) *
                (
                    1d - xWeight
                ) +
                Read(
                    x1,
                    y1,
                    channelOffset
                ) *
                xWeight;

            return
                (float)(
                    top *
                    (
                        1d - yWeight
                    ) +
                    bottom *
                    yWeight
                );
        }

        private byte Read(
            int x,
            int y,
            int channelOffset
        )
        {
            return
                _pixels[
                    y * _stride +
                    x * 4 +
                    channelOffset
                ];
        }
    }
}
