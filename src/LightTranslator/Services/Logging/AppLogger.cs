namespace LightTranslator.Services.Logging;

public sealed class AppLogger
{
    private static readonly HashSet<string> ScreenshotStages =
        new(
            new[]
            {
                "capture",
                "ocr",
                "translation",
                "render"
            },
            StringComparer.Ordinal
        );

    private readonly ILogSink _sink;

    public AppLogger(
        ILogSink sink
    )
    {
        _sink =
            sink;
    }

    public void TranslationCompleted(
        long durationMilliseconds
    )
    {
        _sink.Write(
            $"Translation completed in {durationMilliseconds} ms."
        );
    }

    public void TranslationFailed(
        Exception exception
    )
    {
        _sink.Write(
            $"Translation failed: {exception.GetType().Name}."
        );
    }

    public void ScreenshotStageCompleted(
        string stage,
        long elapsedMilliseconds,
        int blockCount,
        string modelId
    )
    {
        ValidateScreenshotStage(
            stage
        );

        _sink.Write(
            $"Screenshot stage completed: stage={stage}, elapsedMs={elapsedMilliseconds}, blockCount={blockCount}, model={modelId}."
        );
    }

    public void ScreenshotStageFailed(
        string stage,
        string exceptionType
    )
    {
        ValidateScreenshotStage(
            stage
        );

        ArgumentException.ThrowIfNullOrWhiteSpace(
            exceptionType
        );

        _sink.Write(
            $"Screenshot stage failed: stage={stage}, exception={exceptionType}."
        );
    }

    private static void ValidateScreenshotStage(
        string stage
    )
    {
        if (!ScreenshotStages.Contains(stage))
        {
            throw new ArgumentException(
                "Unsupported screenshot stage.",
                nameof(stage)
            );
        }
    }
}
