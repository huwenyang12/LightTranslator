using System.Diagnostics;
using LightTranslator.Models;
using LightTranslator.Services.Logging;
using LightTranslator.Services.Ocr;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;

namespace LightTranslator.Services.Screenshot;

public sealed class ScreenshotTranslationCoordinator
    : IDisposable
{
    private const double MinimumOcrConfidence =
        0.50;

    private const string OcrModelId =
        "ppocrv5-mobile-universal";

    private readonly object _stateLock =
        new();

    private readonly IDisplayCaptureService _displayCaptureService;
    private readonly IScreenshotCaptureView _captureView;
    private readonly IOcrService _ocrService;
    private readonly IScreenshotTextTranslator _textTranslator;
    private readonly IScreenshotResultViewFactory _resultViewFactory;
    private readonly ISettingsService _settingsService;
    private readonly AppLogger? _logger;

    private CancellationTokenSource? _activeCancellation;
    private IScreenshotResultView? _activeResultView;
    private EventHandler? _activeCloseHandler;
    private long _generation;
    private bool _disposed;

    public ScreenshotTranslationCoordinator(
        IDisplayCaptureService displayCaptureService,
        IScreenshotCaptureView captureView,
        IOcrService ocrService,
        IScreenshotTextTranslator textTranslator,
        IScreenshotResultViewFactory resultViewFactory,
        ISettingsService settingsService,
        AppLogger? logger = null
    )
    {
        _displayCaptureService =
            displayCaptureService ??
            throw new ArgumentNullException(
                nameof(displayCaptureService)
            );

        _captureView =
            captureView ??
            throw new ArgumentNullException(
                nameof(captureView)
            );

        _ocrService =
            ocrService ??
            throw new ArgumentNullException(
                nameof(ocrService)
            );

        _textTranslator =
            textTranslator ??
            throw new ArgumentNullException(
                nameof(textTranslator)
            );

        _resultViewFactory =
            resultViewFactory ??
            throw new ArgumentNullException(
                nameof(resultViewFactory)
            );

        _settingsService =
            settingsService ??
            throw new ArgumentNullException(
                nameof(settingsService)
            );

        _logger =
            logger;
    }

    public void Toggle()
    {
        CancellationTokenSource cancellation;
        long generation;

        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(
                _disposed,
                this
            );

            if (_activeCancellation is not null)
            {
                cancellation =
                    null!;

                generation =
                    0;
            }
            else
            {
                cancellation =
                    new CancellationTokenSource();

                _activeCancellation =
                    cancellation;

                generation =
                    ++_generation;
            }
        }

        if (cancellation is null)
        {
            CancelActive();

            return;
        }

        _ =
            RunAsync(
                generation,
                cancellation.Token
            );
    }

    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed =
                true;
        }

        CancelActive();

        if (_ocrService is IDisposable disposableOcr)
        {
            disposableOcr.Dispose();
        }
    }

    private async Task RunAsync(
        long generation,
        CancellationToken cancellationToken
    )
    {
        IScreenshotResultView? resultView =
            null;

        var stage =
            WorkflowStage.Capture;

        var stageTimer =
            Stopwatch.StartNew();

        try
        {
            var frame =
                _displayCaptureService.CaptureMonitorAtCursor();

            var selectedBounds =
                await _captureView.SelectAsync(
                    frame,
                    cancellationToken
                );

            if (!IsCurrent(
                    generation,
                    cancellationToken
                ))
            {
                return;
            }

            if (selectedBounds is null)
            {
                LogStageCompleted(
                    WorkflowStage.Capture,
                    stageTimer.ElapsedMilliseconds,
                    0
                );

                CompleteWithoutView(
                    generation
                );

                return;
            }

            var selection =
                _displayCaptureService.Crop(
                    frame,
                    selectedBounds.Value
                );

            LogStageCompleted(
                WorkflowStage.Capture,
                stageTimer.ElapsedMilliseconds,
                0
            );

            resultView =
                _resultViewFactory.Create(
                    selection
                );

            if (!AttachResultView(
                    generation,
                    resultView
                ))
            {
                resultView.Close();

                return;
            }

            resultView.ShowLoading(
                selection,
                "正在识别…"
            );

            stage =
                WorkflowStage.Ocr;

            stageTimer.Restart();

            var recognized =
                await _ocrService.RecognizeAsync(
                    selection.Image,
                    cancellationToken
                );

            if (!IsCurrent(
                    generation,
                    cancellationToken
                ))
            {
                return;
            }

            var blocks =
                OcrBlock.FilterAndSort(
                    recognized,
                    MinimumOcrConfidence
                );

            LogStageCompleted(
                WorkflowStage.Ocr,
                stageTimer.ElapsedMilliseconds,
                blocks.Count
            );

            if (blocks.Count == 0)
            {
                resultView.ShowMessage(
                    "未识别到文字"
                );

                return;
            }

            var paragraphs =
                OcrParagraphGrouper.Group(
                    blocks
                );

            stage =
                WorkflowStage.Translation;

            stageTimer.Restart();

            var settings =
                await _settingsService.LoadAsync(
                    cancellationToken
                );

            if (!IsCurrent(
                    generation,
                    cancellationToken
                ))
            {
                return;
            }

            var translations =
                await _textTranslator.TranslateAsync(
                    paragraphs,
                    settings.ScreenshotSourceLanguage,
                    settings.ScreenshotTargetLanguage,
                    cancellationToken
                );

            if (!IsCurrent(
                    generation,
                    cancellationToken
                ))
            {
                return;
            }

            var translatedBlocks =
                paragraphs
                    .Select(
                        paragraph =>
                        {
                            if (!translations.TryGetValue(
                                    paragraph.Id,
                                    out var translatedText
                                ) ||
                                string.IsNullOrWhiteSpace(
                                    translatedText
                                ))
                            {
                                return paragraph;
                            }

                            return
                                paragraph with
                                {
                                    TranslatedText =
                                        translatedText
                                };
                        }
                    )
                    .Where(
                        paragraph =>
                            !string.IsNullOrWhiteSpace(
                                paragraph.TranslatedText
                            )
                    )
                    .ToArray();

            LogStageCompleted(
                WorkflowStage.Translation,
                stageTimer.ElapsedMilliseconds,
                translatedBlocks.Length
            );

            stage =
                WorkflowStage.Render;

            stageTimer.Restart();

            resultView.ShowResults(
                translatedBlocks
            );

            LogStageCompleted(
                WorkflowStage.Render,
                stageTimer.ElapsedMilliseconds,
                translatedBlocks.Length
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (OcrModelException exception)
        {
            LogStageFailed(
                WorkflowStage.Ocr,
                exception
            );

            ShowMessageIfCurrent(
                generation,
                resultView,
                "文字识别失败，请重试"
            );
        }
        catch (TranslationException exception)
        {
            LogStageFailed(
                WorkflowStage.Translation,
                exception
            );

            ShowMessageIfCurrent(
                generation,
                resultView,
                MapTranslationFailure(
                    exception.Kind
                )
            );
        }
        catch (Exception exception)
        {
            LogStageFailed(
                stage,
                exception
            );

            var message =
                stage == WorkflowStage.Ocr
                    ? "文字识别失败，请重试"
                    : "截图翻译失败，请重试";

            if (!ShowMessageIfCurrent(
                    generation,
                    resultView,
                    message
                ))
            {
                CompleteWithoutView(
                    generation
                );
            }
        }
    }

    private void LogStageCompleted(
        WorkflowStage stage,
        long elapsedMilliseconds,
        int blockCount
    )
    {
        _logger?.ScreenshotStageCompleted(
            GetStageName(stage),
            elapsedMilliseconds,
            blockCount,
            OcrModelId
        );
    }

    private void LogStageFailed(
        WorkflowStage stage,
        Exception exception
    )
    {
        _logger?.ScreenshotStageFailed(
            GetStageName(stage),
            exception.GetType().Name
        );
    }

    private static string GetStageName(
        WorkflowStage stage
    )
    {
        return
            stage switch
            {
                WorkflowStage.Capture =>
                    "capture",

                WorkflowStage.Ocr =>
                    "ocr",

                WorkflowStage.Translation =>
                    "translation",

                WorkflowStage.Render =>
                    "render",

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(stage),
                        stage,
                        null
                    )
            };
    }

    private bool AttachResultView(
        long generation,
        IScreenshotResultView resultView
    )
    {
        EventHandler closeHandler =
            (_, _) =>
                CancelActive(
                    generation
                );

        lock (_stateLock)
        {
            if (!IsCurrentLocked(
                    generation
                ))
            {
                return false;
            }

            _activeResultView =
                resultView;

            _activeCloseHandler =
                closeHandler;

            resultView.CloseRequested +=
                closeHandler;

            return true;
        }
    }

    private bool ShowMessageIfCurrent(
        long generation,
        IScreenshotResultView? resultView,
        string message
    )
    {
        if (
            resultView is null ||
            !IsCurrent(
                generation,
                CancellationToken.None
            )
        )
        {
            return false;
        }

        resultView.ShowMessage(
            message
        );

        return true;
    }

    private bool IsCurrent(
        long generation,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        lock (_stateLock)
        {
            return
                IsCurrentLocked(
                    generation
                );
        }
    }

    private bool IsCurrentLocked(
        long generation
    )
    {
        return
            !_disposed &&
            _activeCancellation is not null &&
            _generation == generation;
    }

    private void CompleteWithoutView(
        long generation
    )
    {
        CancellationTokenSource? cancellation =
            null;

        lock (_stateLock)
        {
            if (
                !IsCurrentLocked(
                    generation
                ) ||
                _activeResultView is not null
            )
            {
                return;
            }

            cancellation =
                _activeCancellation;

            _activeCancellation =
                null;

            _generation++;
        }

        cancellation?.Dispose();
    }

    private void CancelActive(
        long? expectedGeneration = null
    )
    {
        CancellationTokenSource? cancellation;
        IScreenshotResultView? resultView;
        EventHandler? closeHandler;

        lock (_stateLock)
        {
            if (
                _activeCancellation is null ||
                (
                    expectedGeneration.HasValue &&
                    _generation != expectedGeneration.Value
                )
            )
            {
                return;
            }

            cancellation =
                _activeCancellation;

            resultView =
                _activeResultView;

            closeHandler =
                _activeCloseHandler;

            _activeCancellation =
                null;

            _activeResultView =
                null;

            _activeCloseHandler =
                null;

            _generation++;
        }

        if (
            resultView is not null &&
            closeHandler is not null
        )
        {
            resultView.CloseRequested -=
                closeHandler;
        }

        cancellation.Cancel();

        resultView?.Close();

        cancellation.Dispose();
    }

    private static string MapTranslationFailure(
        TranslationErrorKind kind
    )
    {
        return
            kind switch
            {
                TranslationErrorKind.Configuration =>
                    "请先在设置中配置 API Key",

                TranslationErrorKind.InvalidApiKey =>
                    "请先在设置中配置 API Key",

                TranslationErrorKind.Network =>
                    "翻译失败，请检查网络后重试",

                TranslationErrorKind.Timeout =>
                    "翻译失败，请检查网络后重试",

                _ =>
                    "翻译失败，请重试"
            };
    }

    private enum WorkflowStage
    {
        Capture,
        Ocr,
        Translation,
        Render
    }
}
