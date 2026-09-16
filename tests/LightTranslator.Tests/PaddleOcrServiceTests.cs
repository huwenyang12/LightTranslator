using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class PaddleOcrServiceTests
{
    [Fact]
    public async Task RecognizeAsync_WhenAlreadyCancelled_DoesNotLoadModels()
    {
        var provider =
            new RecordingModelProvider();

        using var service =
            new PaddleOcrService(provider);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () =>
                service.RecognizeAsync(
                    CreateBlankBitmap(),
                    cancellation.Token
                )
        );

        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task RecognizeAsync_WithBundledModels_RecognizesGeneratedText()
    {
        var image =
            RunOnSta(
                () =>
                    CreateTextBitmap(
                        "Hello 你好 日本語"
                    )
            );

        using var service =
            new PaddleOcrService(
                new OcrModelProvider(
                    AppContext.BaseDirectory
                )
            );

        var blocks =
            await service.RecognizeAsync(
                image
            );

        Assert.NotEmpty(blocks);
        Assert.All(
            blocks,
            block =>
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        block.Text
                    )
                );
                Assert.True(block.Confidence >= 0.50);
                Assert.False(block.Bounds.IsEmpty);
            }
        );
    }

    private static BitmapSource CreateBlankBitmap()
    {
        var bitmap =
            new WriteableBitmap(
                32,
                32,
                96,
                96,
                PixelFormats.Bgra32,
                null
            );

        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource CreateTextBitmap(
        string text
    )
    {
        const int width = 720;
        const int height = 140;

        var visual =
            new DrawingVisual();

        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(
                Brushes.White,
                null,
                new Rect(
                    0,
                    0,
                    width,
                    height
                )
            );

            var formatted =
                new FormattedText(
                    text,
                    CultureInfo.GetCultureInfo(
                        "zh-CN"
                    ),
                    FlowDirection.LeftToRight,
                    new Typeface(
                        "Microsoft YaHei UI"
                    ),
                    52,
                    Brushes.Black,
                    1
                );

            drawing.DrawText(
                formatted,
                new Point(
                    24,
                    34
                )
            );
        }

        var bitmap =
            new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32
            );

        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    private static T RunOnSta<T>(
        Func<T> action
    )
    {
        T? result = default;
        Exception? exception = null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        result = action();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new InvalidOperationException(
                "STA operation failed.",
                exception
            );
        }

        return result!;
    }

    private sealed class RecordingModelProvider
        : IOcrModelProvider
    {
        public int CallCount
        {
            get;
            private set;
        }

        public OcrModelPaths GetRequiredPaths()
        {
            CallCount++;

            throw new InvalidOperationException(
                "Models must not be loaded after cancellation."
            );
        }
    }
}
