using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Ocr;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationCoordinatorStructureTests
{
    [Fact]
    public async Task Toggle_TranslatesStructuredRegionsInOneBatch()
    {
        var selection =
            new CapturedSelection(
                CreateBitmap(500, 200),
                new PixelRect(0, 0, 500, 200),
                new PixelRect(0, 0, 500, 200),
                96,
                96
            );

        var displayCapture =
            new FakeDisplayCaptureService(
                new ScreenCaptureFrame(
                    CreateBitmap(500, 200),
                    new PixelRect(0, 0, 500, 200),
                    96,
                    96
                ),
                selection
            );

        var captureView =
            new FakeScreenshotCaptureView(
                new PixelRect(0, 0, 500, 200)
            );

        var ocr =
            new FakeOcrService(
                new[]
                {
                    new OcrBlock(
                        "title",
                        "Paragraph 1",
                        0.98,
                        new PixelRect(20, 10, 150, 40)
                    ),
                    new OcrBlock(
                        "body-1",
                        "Learning is rewarding.",
                        0.96,
                        new PixelRect(20, 56, 300, 24)
                    ),
                    new OcrBlock(
                        "body-2",
                        "Practice every day.",
                        0.95,
                        new PixelRect(20, 86, 320, 24)
                    )
                }
            );

        var translator =
            new FakeScreenshotTextTranslator
            {
                Result =
                    new Dictionary<string, string>
                    {
                        ["region-0001"] = "第1段",
                        ["region-0002"] = "学习很有收获。每天练习。"
                    }
            };

        var resultView =
            new FakeScreenshotResultView();

        using var coordinator =
            new ScreenshotTranslationCoordinator(
                displayCapture,
                captureView,
                ocr,
                translator,
                new FakeScreenshotResultViewFactory(
                    resultView
                ),
                new FakeSettingsService(
                    AppSettings.CreateDefault()
                )
            );

        coordinator.Toggle();

        await resultView.ResultsShown.Task.WaitAsync(
            TimeSpan.FromSeconds(3)
        );

        Assert.Equal(1, translator.CallCount);
        Assert.Equal(2, translator.LastBlocks.Count);
        Assert.Equal(
            "region-0001",
            translator.LastBlocks[0].Id
        );
        Assert.Equal(
            "Paragraph 1",
            translator.LastBlocks[0].Text
        );
        Assert.Equal(
            "region-0002",
            translator.LastBlocks[1].Id
        );
        Assert.Equal(
            "Learning is rewarding. Practice every day.",
            translator.LastBlocks[1].Text
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
                96,
                96,
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

    private sealed class FakeScreenshotCaptureView
        : IScreenshotCaptureView
    {
        private readonly PixelRect? _selection;

        public FakeScreenshotCaptureView(
            PixelRect? selection
        )
        {
            _selection = selection;
        }

        public Task<PixelRect?> SelectAsync(
            ScreenCaptureFrame frame,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                _selection
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

    private sealed class FakeScreenshotTextTranslator
        : IScreenshotTextTranslator
    {
        public IReadOnlyDictionary<string, string> Result { get; set; } =
            new Dictionary<string, string>();

        public int CallCount { get; private set; }

        public IReadOnlyList<OcrBlock> LastBlocks { get; private set; } =
            Array.Empty<OcrBlock>();

        public Task<IReadOnlyDictionary<string, string>> TranslateAsync(
            IReadOnlyList<OcrBlock> blocks,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            LastBlocks = blocks;

            return Task.FromResult(
                Result
            );
        }
    }

    private sealed class FakeScreenshotResultViewFactory
        : IScreenshotResultViewFactory
    {
        private readonly IScreenshotResultView _view;

        public FakeScreenshotResultViewFactory(
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

    private sealed class FakeScreenshotResultView
        : IScreenshotResultView
    {
        public event EventHandler? CloseRequested;

        public TaskCompletionSource ResultsShown { get; } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

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

        public void RaiseCloseRequested()
        {
            CloseRequested?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }

    private sealed class FakeSettingsService
        : ISettingsService
    {
        private readonly AppSettings _settings;

        public FakeSettingsService(
            AppSettings settings
        )
        {
            _settings = settings;
        }

        public Task<AppSettings> LoadAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                _settings
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
