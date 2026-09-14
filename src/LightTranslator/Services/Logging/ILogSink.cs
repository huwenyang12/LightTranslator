namespace LightTranslator.Services.Logging;

public interface ILogSink
{
    void Write(
        string message
    );
}