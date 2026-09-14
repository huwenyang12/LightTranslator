using System.IO;

namespace LightTranslator.Services.Logging;

public sealed class FileLogSink
    : ILogSink
{
    private readonly string _logFilePath;


    public FileLogSink(
        string logFilePath
    )
    {
        _logFilePath =
            logFilePath;
    }


    public void Write(
        string message
    )
    {
        var directory =
            Path.GetDirectoryName(
                _logFilePath
            );

        if (
            !string.IsNullOrWhiteSpace(
                directory
            )
        )
        {
            Directory.CreateDirectory(
                directory
            );
        }

        var timestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );

        File.AppendAllText(
            _logFilePath,
            $"[{timestamp}] {message}{Environment.NewLine}"
        );
    }
}