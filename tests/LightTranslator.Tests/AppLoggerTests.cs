using LightTranslator.Services.Logging;

namespace LightTranslator.Tests;

public class AppLoggerTests
{

    [Fact]
    public void TranslationFailed_DoesNotLogExceptionMessage()
    {
        var sink =
            new FakeLogSink();

        var logger =
            new AppLogger(
                sink
            );

        var exception =
            new InvalidOperationException(
                "secret-user-text"
            );

        logger.TranslationFailed(
            exception
        );

        Assert.Single(
            sink.Messages
        );

        Assert.Equal(
            "Translation failed: InvalidOperationException.",
            sink.Messages[0]
        );

        Assert.DoesNotContain(
            "secret-user-text",
            sink.Messages[0]
        );
    }

    [Fact]
    public void TranslationCompleted_LogsOnlySafeMetadata()
    {
        var sink =
            new FakeLogSink();

        var logger =
            new AppLogger(
                sink
            );

        logger.TranslationCompleted(
            durationMilliseconds: 428
        );

        Assert.Single(
            sink.Messages
        );

        Assert.Equal(
            "Translation completed in 428 ms.",
            sink.Messages[0]
        );
    }


    private sealed class FakeLogSink
        : ILogSink
    {
        public List<string> Messages { get; } =
            new();

        public void Write(
            string message
        )
        {
            Messages.Add(
                message
            );
        }
    }
}