using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Ocr;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public sealed class ScreenshotRegionTranslationCoordinatorTests
{
    [Fact]
    public async Task Toggle_TranslatesAndRendersRegionsInsteadOfIndividualOcrLines()
    {
        var frame =
            new ScreenCaptureFrame(
                CreateBitmap(800, 400),
                new PixelRect(0, 0, 800, 400),
                120,
                120
            );

        var selection =
            new CapturedSelection(
                CreateBitmap(700, 300),
                new PixelRect(20, 20, 700, 300),
                new PixelRect(20, 20, 700, 300),
                120,
                120
            );

        var ocrBlocks =
            new[]
            {
                new OcrBlock(
                    "block-0001",
                    "Learning a new language is rewarding.",
                    0.96,
                    new PixelRect(20, 10, 420, 24)
                ),
                new OcrBlock(
                    "block-0002",
                    "It opens doors to new cultures.",
                    0.94,
                    new PixelRect(20, 40, 480, 24)
                ),
                new OcrBlock(
                    "block-0003",
                    "Consistency matters more than intensity.",
                    0.95,
                    new PixelRect(20, 120, 470, 24)
                ),
                new OcrBlock(
                    "block-0004",
                    "Practice every day to improve steadily.",
                    0.93,
                    new PixelRect(20, 150, 460, 24)
                )
            };

        var translator =
            new RecordingTranslator();

        var resultView =
            new RecordingResultView();

        using var coordinator =
            new ScreenshotTranslationCoordinator(
                new FakeDisplayCaptureService(
                    frame,
                    selection
                ),
                new FakeCaptureView(),
                new FakeOcrService(
                    ocrBlocks
                ),
                translator,
                new FakeResultViewFactory(
                    resultView
                ),
                new FakeSettingsService()
            );

        coordinator.Toggle();

        await resultView.ResultsShown.Task.WaitAsync(
            TimeSpan.FromSeconds(3)
        );

        Assert.Equal(
            new[]
            {
                "region-0001",
                "region-0002"
            },
            translator.LastBlocks
                .Select(block => block.Id)
        );

        Assert.Equal(
            new[]
            {
                "Learning a new language is rewarding. It opens doors to new cultures.",
                "Consistency matters more than intensity. Practice every day to improve steadily."
            },
            translator.LastBlocks
                .Select(block => block.Text)
        );

        Assert.Equal(
            2,
            resultView.LastResults.Count
        );

        Assert.Equal(
            new[]
            {
                "region-0001",
                "region-0002"
            },
            resultView.LastResults
                .Select(block => block.Id)
        );

        Assert.All(
            resultView.LastResults,
            block =>
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        block.TranslatedText
                    )
                )
        );
    }

    private static BitmapSource CreateBitmap(
        int width,
        int height
    )
    {
        var bitmap =
            new WriteableBitmap(
                width,
                height,
                120,
                120,
                PixelFormats.Bgra32,
                null
            );

        bitmap.Freeze();
        return bitmap;
    }

    private sealed class FakeDisplayCaptureService
        : IDisplayCaptureService
    {
        private readonly ScreenCaptureFrame _frame;
        private readonly CapturedSelection _selection;

        public FakeDisplayCaptureService(
            ScreenCaptureFrame frame,
            CapturedSelection selection
        )
        {
            _frame = frame;
            _selection = selection;
        }

        public ScreenCaptureFrame CaptureMonitorAtCursor()
        {
            return _frame;
        }

        public CapturedSelection Crop(
            ScreenCaptureFrame frame,
            PixelRect monitorRelativeBounds
        )
        {
            return _selection;
        }
    }

    private sealed class FakeCaptureView
        : IScreenshotCaptureView
    {
        public Task<PixelRect?> SelectAsync(
            ScreenCaptureFrame frame,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<PixelRect?>(
                new PixelRect(
                    20,
                    20,
                    700,
                    300
                )
            );
        }
    }

    private sealed class FakeOcrService
        : IOcrService
    {
        private readonly IReadOnlyList<OcrBlock> _blocks;

        public FakeOcrService(
            IReadOnlyList<OcrBlock> blocks
        )
        {
            _blocks = blocks;
        }

        public Task<IReadOnlyList<OcrBlock>> RecognizeAsync(
            BitmapSource image,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                _blocks
            );
        }
    }

    private sealed class RecordingTranslator
        : IScreenshotTextTranslator
    {
        public IReadOnlyList<OcrBlock> LastBlocks
        {
            get;
            private set;
        } =
            Array.Empty<OcrBlock>();

        public Task<IReadOnlyDictionary<string, string>> TranslateAsync(
            IReadOnlyList<OcrBlock> blocks,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken = default
        )
        {
            LastBlocks =
                blocks.ToArray();

            IReadOnlyDictionary<string, string> translations =
                blocks.ToDictionary(
                    block => block.Id,
                    block => $"译文:{block.Text}"
                );

            return Task.FromResult(
                translations
            );
        }
    }

    private sealed class FakeResultViewFactory
        : IScreenshotResultViewFactory
    {
        private readonly IScreenshotResultView _view;

        public FakeResultViewFactory(
            IScreenshotResultView view
        )
        {
            _view = view;
        }

        public IScreenshotResultView Create(
            CapturedSelection selection
        )
        {
            return _view;
        }
    }

    private sealed class RecordingResultView
        : IScreenshotResultView
    {
        public event EventHandler? CloseRequested
        {
            add
            {
            }

            remove
            {
            }
        }

        public TaskCompletionSource ResultsShown
        {
            get;
        } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public IReadOnlyList<OcrBlock> LastResults
        {
            get;
            private set;
        } =
            Array.Empty<OcrBlock>();

        public void ShowLoading(
            CapturedSelection selection,
            string message
        )
        {
        }

        public void ShowResults(
            IReadOnlyList<OcrBlock> blocks
        )
        {
            LastResults =
                blocks.ToArray();

            ResultsShown.TrySetResult();
        }

        public void ShowMessage(
            string message
        )
        {
        }

        public void Close()
        {
        }
    }

    private sealed class FakeSettingsService
        : ISettingsService
    {
        public Task<AppSettings> LoadAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                AppSettings.CreateDefault()
            );
        }

        public Task SaveAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
