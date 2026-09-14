namespace LightTranslator.Services.Logging;

public sealed class AppLogger
{
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
}