using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.Ocr;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationCoordinatorTests
{
    [Fact]
    public async Task Toggle_CompletesCaptureOcrTranslationAndRendering()
    {
        var fixture =
            CoordinatorFixture.Create();

        fixture.Ocr.Blocks =
            new[]
            {
                new OcrBlock(
                    "block-0002",
                    "second",
                    0.90,
                    new PixelRect(
                        120,
                        60,
                        100,
                        30
                    )
                ),
                new OcrBlock(
                    "low",
                    "excluded",
                    0.49,
                    new PixelRect(
                        5,
                        5,
                        40,
                        20
                    )
                ),
                new OcrBlock(
                    "block-0001",
                    "first",
                    0.95,
                    new PixelRect(
                        10,
                        10,
                        100,
                        30
                    )
                )
            };

        fixture.Translator.Result =
            new Dictionary<string, string>
            {
                ["block-0001"] =
                    "第一",
                ["block-0002"] =
                    "第二",
                ["unknown"] =
                    "忽略"
            };

        fixture.Coordinator.Toggle();

        await fixture.ResultView.ResultsShown.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        Assert.Equal(
            1,
            fixture.DisplayCapture.CaptureCount
        );

        Assert.Equal(
            1,
            fixture.DisplayCapture.CropCount
        );

        Assert.Equal(
            1,
            fixture.CaptureView.SelectCount
        );

        Assert.Equal(
            1,
            fixture.ResultView.ShowLoadingCount
        );

        Assert.Equal(
            "正在识别…",
            fixture.ResultView.LastLoadingMessage
        );

        Assert.Equal(
            new[]
            {
                "block-0001",
                "block-0002"
            },
            fixture.Translator.LastBlocks
                .Select(
                    block => block.Id
                )
        );

        Assert.Equal(
            "auto",
            fixture.Translator.LastSourceLanguage
        );

        Assert.Equal(
            "zh",
            fixture.Translator.LastTargetLanguage
        );

        Assert.Equal(
            new[]
            {
                "第一",
                "第二"
            },
            fixture.ResultView.LastResults
                .Select(
                    block =>
                        block.TranslatedText
                )
        );
    }

    [Fact]
    public async Task Toggle_WhenTaskActive_CancelsAndClosesInsteadOfStartingAnother()
    {
        var fixture =
            CoordinatorFixture.Create();

        fixture.Ocr.Handler =
            async cancellationToken =>
            {
                fixture.Ocr.Started.TrySetResult();

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken
                );

                return Array.Empty<OcrBlock>();
            };

        fixture.Coordinator.Toggle();

        await fixture.Ocr.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        fixture.Coordinator.Toggle();

        await WaitUntilAsync(
            () =>
                fixture.Ocr.LastCancellationToken
                    .IsCancellationRequested
        );

        Assert.Equal(
            1,
            fixture.DisplayCapture.CaptureCount
        );

        Assert.Equal(
            1,
            fixture.CaptureView.SelectCount
        );

        Assert.Equal(
            1,
            fixture.ResultView.CloseCount
        );
    }

    [Fact]
    public async Task CancelledLateTranslation_CannotUpdateWindow()
    {
        var fixture =
            CoordinatorFixture.Create();

        var lateTranslation =
            new TaskCompletionSource<IReadOnlyDictionary<string, string>>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        fixture.Translator.Handler =
            (
                blocks,
                sourceLanguage,
                targetLanguage,
                cancellationToken
            ) =>
            {
                fixture.Translator.Started.TrySetResult();

                return lateTranslation.Task;
            };

        fixture.Coordinator.Toggle();

        await fixture.Translator.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        fixture.Coordinator.Toggle();

        Assert.Equal(
            1,
            fixture.ResultView.CloseCount
        );

        lateTranslation.SetResult(
            new Dictionary<string, string>
            {
                ["block-0001"] =
                    "迟到"
            }
        );

        await Task.Delay(
            100
        );

        Assert.Equal(
            0,
            fixture.ResultView.ShowResultsCount
        );

        Assert.Equal(
            1,
            fixture.ResultView.CloseCount
        );
    }

    [Fact]
    public async Task CloseRequested_CancelsActiveWorkAndClosesView()
    {
        var fixture =
            CoordinatorFixture.Create();

        fixture.Ocr.Handler =
            async cancellationToken =>
            {
                fixture.Ocr.Started.TrySetResult();

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken
                );

                return Array.Empty<OcrBlock>();
            };

        fixture.Coordinator.Toggle();

        await fixture.Ocr.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        fixture.ResultView.RaiseCloseRequested();

        await WaitUntilAsync(
            () =>
                fixture.Ocr.LastCancellationToken
                    .IsCancellationRequested
        );

        Assert.Equal(
            1,
            fixture.ResultView.CloseCount
        );
    }

    [Theory]
    [InlineData(
        "NoText",
        "未识别到文字"
    )]
    [InlineData(
        "Ocr",
        "文字识别失败，请重试"
    )]
    [InlineData(
        "MissingApiKey",
        "请先在设置中配置 API Key"
    )]
    [InlineData(
        "Network",
        "翻译失败，请检查网络后重试"
    )]
    public async Task Failure_ShowsExpectedOverlayMessage(
        string failure,
        string expected
    )
    {
        var fixture =
            CoordinatorFixture.Create();

        switch (failure)
        {
            case "NoText":
                fixture.Ocr.Blocks =
                    Array.Empty<OcrBlock>();

                break;

            case "Ocr":
                fixture.Ocr.Exception =
                    new OcrModelException(
                        "ModelUnavailable"
                    );

                break;

            case "MissingApiKey":
                fixture.Translator.Exception =
                    new TranslationException(
                        TranslationErrorKind.Configuration,
                        "Key missing."
                    );

                break;

            case "Network":
                fixture.Translator.Exception =
                    new TranslationException(
                        TranslationErrorKind.Network,
                        "Network failed."
                    );

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(
                        failure
                    )
                );
        }

        fixture.Coordinator.Toggle();

        await fixture.ResultView.MessageShown.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        Assert.Equal(
            expected,
            fixture.ResultView.LastMessage
        );
    }

    [Fact]
    public async Task Dispose_CancelsWorkClosesViewAndDisposesOcr()
    {
        var fixture =
            CoordinatorFixture.Create();

        fixture.Ocr.Handler =
            async cancellationToken =>
            {
                fixture.Ocr.Started.TrySetResult();

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken
                );

                return Array.Empty<OcrBlock>();
            };

        fixture.Coordinator.Toggle();

        await fixture.Ocr.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(
                3
            )
        );

        fixture.Coordinator.Dispose();

        await WaitUntilAsync(
            () =>
                fixture.Ocr.LastCancellationToken
                    .IsCancellationRequested
        );

        Assert.Equal(
            1,
            fixture.ResultView.CloseCount
        );

        Assert.Equal(
            1,
            fixture.Ocr.DisposeCount
        );
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition
    )
    {
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    3
                )
            );

        while (!condition())
        {
            await Task.Delay(
                10,
                timeout.Token
            );
        }
    }

    private sealed class CoordinatorFixture
    {
        private CoordinatorFixture()
        {
            var frame =
                new ScreenCaptureFrame(
                    CreateBitmap(
                        400,
                        200
                    ),
                    new PixelRect(
                        0,
                        0,
                        400,
                        200
                    ),
                    120,
                    120
                );

            var selection =
                new CapturedSelection(
                    CreateBitmap(
                        300,
                        150
                    ),
                    new PixelRect(
                        20,
                        20,
                        300,
                        150
                    ),
                    new PixelRect(
                        20,
                        20,
                        300,
                        150
                    ),
                    120,
                    120
                );

            DisplayCapture =
                new FakeDisplayCaptureService(
                    frame,
                    selection
                );

            CaptureView =
                new FakeScreenshotCaptureView(
                    new PixelRect(
                        20,
                        20,
                        300,
                        150
                    )
                );

            Ocr =
                new FakeOcrService
                {
                    Blocks =
                        new[]
                        {
                            new OcrBlock(
                                "block-0001",
                                "source",
                                0.90,
                                new PixelRect(
                                    10,
                                    10,
                                    100,
                                    30
                                )
                            )
                        }
                };

            Translator =
                new FakeScreenshotTextTranslator
                {
                    Result =
                        new Dictionary<string, string>
                        {
                            ["block-0001"] =
                                "译文"
                        }
                };

            ResultView =
                new FakeScreenshotResultView();

            ResultFactory =
                new FakeScreenshotResultViewFactory(
                    ResultView
                );

            Settings =
                new FakeSettingsService(
                    AppSettings.CreateDefault()
                );

            Coordinator =
                new ScreenshotTranslationCoordinator(
                    DisplayCapture,
                    CaptureView,
                    Ocr,
                    Translator,
                    ResultFactory,
                    Settings
                );
        }

        public ScreenshotTranslationCoordinator Coordinator
        {
            get;
        }

        public FakeDisplayCaptureService DisplayCapture
        {
            get;
        }

        public FakeScreenshotCaptureView CaptureView
        {
            get;
        }

        public FakeOcrService Ocr
        {
            get;
        }

        public FakeScreenshotTextTranslator Translator
        {
            get;
        }

        public FakeScreenshotResultView ResultView
        {
            get;
        }

        public FakeScreenshotResultViewFactory ResultFactory
        {
            get;
        }

        public FakeSettingsService Settings
        {
            get;
        }

        public static CoordinatorFixture Create()
        {
            return
                new CoordinatorFixture();
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
            _frame =
                frame;

            _selection =
                selection;
        }

        public int CaptureCount
        {
            get;
            private set;
        }

        public int CropCount
        {
            get;
            private set;
        }

        public ScreenCaptureFrame CaptureMonitorAtCursor()
        {
            CaptureCount++;

            return _frame;
        }

        public CapturedSelection Crop(
            ScreenCaptureFrame frame,
            PixelRect monitorRelativeBounds
        )
        {
            CropCount++;

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
            _selection =
                selection;
        }

        public int SelectCount
        {
            get;
            private set;
        }

        public Task<PixelRect?> SelectAsync(
            ScreenCaptureFrame frame,
            CancellationToken cancellationToken = default
        )
        {
            SelectCount++;

            return
                Task.FromResult(
                    _selection
                );
        }
    }

    private sealed class FakeOcrService
        : IOcrService,
          IDisposable
    {
        public IReadOnlyList<OcrBlock> Blocks
        {
            get;
            set;
        } =
            Array.Empty<OcrBlock>();

        public Exception? Exception
        {
            get;
            set;
        }

        public Func<CancellationToken, Task<IReadOnlyList<OcrBlock>>>? Handler
        {
            get;
            set;
        }

        public TaskCompletionSource Started
        {
            get;
        } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public int DisposeCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<OcrBlock>> RecognizeAsync(
            BitmapSource image,
            CancellationToken cancellationToken = default
        )
        {
            LastCancellationToken =
                cancellationToken;

            if (Handler is not null)
            {
                return
                    Handler(
                        cancellationToken
                    );
            }

            if (Exception is not null)
            {
                return
                    Task.FromException<IReadOnlyList<OcrBlock>>(
                        Exception
                    );
            }

            return
                Task.FromResult(
                    Blocks
                );
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class FakeScreenshotTextTranslator
        : IScreenshotTextTranslator
    {
        public IReadOnlyDictionary<string, string> Result
        {
            get;
            set;
        } =
            new Dictionary<string, string>();

        public Exception? Exception
        {
            get;
            set;
        }

        public Func<
            IReadOnlyList<OcrBlock>,
            string,
            string,
            CancellationToken,
            Task<IReadOnlyDictionary<string, string>>>? Handler
        {
            get;
            set;
        }

        public TaskCompletionSource Started
        {
            get;
        } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public IReadOnlyList<OcrBlock> LastBlocks
        {
            get;
            private set;
        } =
            Array.Empty<OcrBlock>();

        public string? LastSourceLanguage
        {
            get;
            private set;
        }

        public string? LastTargetLanguage
        {
            get;
            private set;
        }

        public Task<IReadOnlyDictionary<string, string>> TranslateAsync(
            IReadOnlyList<OcrBlock> blocks,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken = default
        )
        {
            LastBlocks =
                blocks;

            LastSourceLanguage =
                sourceLanguage;

            LastTargetLanguage =
                targetLanguage;

            if (Handler is not null)
            {
                return
                    Handler(
                        blocks,
                        sourceLanguage,
                        targetLanguage,
                        cancellationToken
                    );
            }

            if (Exception is not null)
            {
                return
                    Task.FromException<IReadOnlyDictionary<string, string>>(
                        Exception
                    );
            }

            return
                Task.FromResult(
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
            _view =
                view;
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

        public int ShowLoadingCount
        {
            get;
            private set;
        }

        public int ShowResultsCount
        {
            get;
            private set;
        }

        public int CloseCount
        {
            get;
            private set;
        }

        public string? LastLoadingMessage
        {
            get;
            private set;
        }

        public string? LastMessage
        {
            get;
            private set;
        }

        public IReadOnlyList<OcrBlock> LastResults
        {
            get;
            private set;
        } =
            Array.Empty<OcrBlock>();

        public TaskCompletionSource ResultsShown
        {
            get;
        } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public TaskCompletionSource MessageShown
        {
            get;
        } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public void ShowLoading(
            CapturedSelection selection,
            string message
        )
        {
            ShowLoadingCount++;

            LastLoadingMessage =
                message;
        }

        public void ShowResults(
            IReadOnlyList<OcrBlock> blocks
        )
        {
            ShowResultsCount++;

            LastResults =
                blocks;

            ResultsShown.TrySetResult();
        }

        public void ShowMessage(
            string message
        )
        {
            LastMessage =
                message;

            MessageShown.TrySetResult();
        }

        public void Close()
        {
            CloseCount++;
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
            _settings =
                settings;
        }

        public Task<AppSettings> LoadAsync(
            CancellationToken cancellationToken = default
        )
        {
            return
                Task.FromResult(
                    _settings
                );
        }

        public Task SaveAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default
        )
        {
            return
                Task.CompletedTask;
        }
    }
}
