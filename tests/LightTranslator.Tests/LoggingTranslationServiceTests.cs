using LightTranslator.Models;
using LightTranslator.Services.Logging;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public class LoggingTranslationServiceTests
{

    [Fact]
    public async Task TranslateAsync_OnCancellation_DoesNotLogFailureAndRethrows()
    {
        var expectedException =
            new OperationCanceledException();

        var innerService =
            new FailingTranslationService(
                expectedException
            );

        var sink =
            new FakeLogSink();

        var logger =
            new AppLogger(
                sink
            );

        var service =
            new LoggingTranslationService(
                innerService,
                logger
            );

        var request =
            new TranslationRequest(
                "secret-source-text",
                "auto",
                "zh"
            );

        var actualException =
            await Assert.ThrowsAsync<OperationCanceledException>(
                () =>
                    service.TranslateAsync(
                        request
                    )
            );

        Assert.Same(
            expectedException,
            actualException
        );

        Assert.Empty(
            sink.Messages
        );
    }

    [Fact]
    public async Task TranslateAsync_OnFailure_LogsSafeErrorAndRethrows()
    {
        var expectedException =
            new InvalidOperationException(
                "secret-error-message"
            );

        var innerService =
            new FailingTranslationService(
                expectedException
            );

        var sink =
            new FakeLogSink();

        var logger =
            new AppLogger(
                sink
            );

        var service =
            new LoggingTranslationService(
                innerService,
                logger
            );

        var request =
            new TranslationRequest(
                "secret-source-text",
                "auto",
                "zh"
            );

        var actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    service.TranslateAsync(
                        request
                    )
            );

        Assert.Same(
            expectedException,
            actualException
        );

        Assert.Single(
            sink.Messages
        );

        Assert.Equal(
            "Translation failed: InvalidOperationException.",
            sink.Messages[0]
        );

        Assert.DoesNotContain(
            "secret-error-message",
            sink.Messages[0]
        );

        Assert.DoesNotContain(
            "secret-source-text",
            sink.Messages[0]
        );
    }


    [Fact]
    public async Task TranslateAsync_OnSuccess_LogsCompletionWithoutText()
    {
        var innerService =
            new FakeTranslationService();

        var sink =
            new FakeLogSink();

        var logger =
            new AppLogger(
                sink
            );

        var service =
            new LoggingTranslationService(
                innerService,
                logger
            );

        var request =
            new TranslationRequest(
                "secret-source-text",
                "auto",
                "zh"
            );

        var result =
            await service.TranslateAsync(
                request
            );

        Assert.Equal(
            "translated-text",
            result.Text
        );

        Assert.Single(
            sink.Messages
        );

        Assert.DoesNotContain(
            "secret-source-text",
            sink.Messages[0]
        );

        Assert.DoesNotContain(
            "translated-text",
            sink.Messages[0]
        );
    }


    private sealed class FakeTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult(
                    "translated-text",
                    null
                )
            );
        }
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

    private sealed class FailingTranslationService
    : ITranslationService
{
    private readonly Exception _exception;


    public FailingTranslationService(
            Exception exception
        )
        {
            _exception =
                exception;
        }


        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromException<TranslationResult>(
                _exception
            );
        }
    }
}