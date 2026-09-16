using System.Net;
using System.Text;
using System.Text.Json;
using LightTranslator.Models;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public sealed class DeepSeekScreenshotTextTranslatorTests
{
    [Fact]
    public async Task TranslateAsync_SendsOnlyIdsTextAndLanguages()
    {
        var handler =
            new RecordingHandler(
                JsonResponse(
                    """
                    {"translations":[{"id":"block-0001","text":"你好"}]}
                    """
                )
            );

        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(handler),
                () => "sk-private-value"
            );

        var blocks =
            new[]
            {
                new OcrBlock(
                    "block-0001",
                    "Hello",
                    0.99,
                    new PixelRect(
                        10,
                        20,
                        80,
                        30
                    )
                )
            };

        var result =
            await translator.TranslateAsync(
                blocks,
                "en",
                "zh"
            );

        Assert.Equal(
            "你好",
            result["block-0001"]
        );

        Assert.Equal(
            "Bearer",
            handler.AuthorizationScheme
        );

        Assert.Equal(
            "sk-private-value",
            handler.AuthorizationParameter
        );

        Assert.NotNull(
            handler.RequestBody
        );

        Assert.DoesNotContain(
            "sk-private-value",
            handler.RequestBody
        );

        using var requestDocument =
            JsonDocument.Parse(
                handler.RequestBody!
            );

        var messages =
            requestDocument
                .RootElement
                .GetProperty(
                    "messages"
                );

        var systemPrompt =
            messages[0]
                .GetProperty(
                    "content"
                )
                .GetString();

        Assert.Contains(
            "Source language: en",
            systemPrompt
        );

        Assert.Contains(
            "Target language: zh",
            systemPrompt
        );

        var userContent =
            messages[1]
                .GetProperty(
                    "content"
                )
                .GetString();

        Assert.NotNull(
            userContent
        );

        using var batchDocument =
            JsonDocument.Parse(
                userContent!
            );

        var item =
            Assert.Single(
                batchDocument
                    .RootElement
                    .EnumerateArray()
            );

        Assert.Equal(
            new[]
            {
                "id",
                "text"
            },
            item
                .EnumerateObject()
                .Select(
                    property =>
                        property.Name
                )
                .ToArray()
        );

        Assert.Equal(
            "block-0001",
            item
                .GetProperty(
                    "id"
                )
                .GetString()
        );

        Assert.Equal(
            "Hello",
            item
                .GetProperty(
                    "text"
                )
                .GetString()
        );

        Assert.DoesNotContain(
            "Bounds",
            userContent
        );

        Assert.DoesNotContain(
            "Confidence",
            userContent
        );

        Assert.DoesNotContain(
            "TranslatedText",
            userContent
        );
    }

    [Fact]
    public async Task TranslateAsync_MapsStrictlyByRequestedId()
    {
        var translator =
            CreateTranslator(
                """
                {
                  "translations": [
                    {"id":"block-0002","text":"第二"},
                    {"id":"unknown","text":"忽略"},
                    {"id":"block-0002","text":"重复值"}
                  ]
                }
                """
            );

        var result =
            await translator.TranslateAsync(
                TwoBlocks(),
                "auto",
                "zh"
            );

        Assert.False(
            result.ContainsKey(
                "block-0001"
            )
        );

        Assert.Equal(
            "第二",
            result["block-0002"]
        );

        Assert.False(
            result.ContainsKey(
                "unknown"
            )
        );

        Assert.Single(
            result
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenBlocksAreEmpty_ReturnsWithoutRequest()
    {
        var handler =
            new RecordingHandler(
                JsonResponse(
                    """
                    {"translations":[]}
                    """
                )
            );

        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(handler),
                () => "test-key"
            );

        var result =
            await translator.TranslateAsync(
                Array.Empty<OcrBlock>(),
                "auto",
                "zh"
            );

        Assert.Empty(
            result
        );

        Assert.Equal(
            0,
            handler.SendCount
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenApiKeyMissing_ThrowsConfigurationError()
    {
        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new RecordingHandler(
                        JsonResponse(
                            """
                            {"translations":[]}
                            """
                        )
                    )
                ),
                () => null
            );

        var error =
            await Assert.ThrowsAsync<TranslationException>(
                () =>
                    translator.TranslateAsync(
                        TwoBlocks(),
                        "auto",
                        "zh"
                    )
            );

        Assert.Equal(
            TranslationErrorKind.Configuration,
            error.Kind
        );
    }

    [Theory]
    [InlineData(
        HttpStatusCode.Unauthorized,
        TranslationErrorKind.InvalidApiKey
    )]
    [InlineData(
        HttpStatusCode.PaymentRequired,
        TranslationErrorKind.InsufficientBalance
    )]
    [InlineData(
        HttpStatusCode.TooManyRequests,
        TranslationErrorKind.RateLimited
    )]
    public async Task TranslateAsync_MapsKnownHttpFailures(
        HttpStatusCode statusCode,
        TranslationErrorKind expectedKind
    )
    {
        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new RecordingHandler(
                        new HttpResponseMessage(
                            statusCode
                        )
                        {
                            Content =
                                new StringContent(
                                    "{}",
                                    Encoding.UTF8,
                                    "application/json"
                                )
                        }
                    )
                ),
                () => "test-key"
            );

        var error =
            await Assert.ThrowsAsync<TranslationException>(
                () =>
                    translator.TranslateAsync(
                        TwoBlocks(),
                        "auto",
                        "zh"
                    )
            );

        Assert.Equal(
            expectedKind,
            error.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenResponseIsMalformed_ThrowsInvalidResponse()
    {
        var translator =
            CreateTranslator(
                """
                {"unexpected":[]}
                """
            );

        var error =
            await Assert.ThrowsAsync<TranslationException>(
                () =>
                    translator.TranslateAsync(
                        TwoBlocks(),
                        "auto",
                        "zh"
                    )
            );

        Assert.Equal(
            TranslationErrorKind.InvalidResponse,
            error.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenRequestTimesOut_ThrowsTimeout()
    {
        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new ThrowingHandler(
                        new TaskCanceledException(
                            "Simulated timeout."
                        )
                    )
                ),
                () => "test-key"
            );

        var error =
            await Assert.ThrowsAsync<TranslationException>(
                () =>
                    translator.TranslateAsync(
                        TwoBlocks(),
                        "auto",
                        "zh"
                    )
            );

        Assert.Equal(
            TranslationErrorKind.Timeout,
            error.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenNetworkFails_ThrowsNetwork()
    {
        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new ThrowingHandler(
                        new HttpRequestException(
                            "Simulated network failure."
                        )
                    )
                ),
                () => "test-key"
            );

        var error =
            await Assert.ThrowsAsync<TranslationException>(
                () =>
                    translator.TranslateAsync(
                        TwoBlocks(),
                        "auto",
                        "zh"
                    )
            );

        Assert.Equal(
            TranslationErrorKind.Network,
            error.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new CancellationHandler()
                ),
                () => "test-key"
            );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () =>
                translator.TranslateAsync(
                    TwoBlocks(),
                    "auto",
                    "zh",
                    cancellation.Token
                )
        );
    }

    [Fact]
    public void DeepSeekScreenshotTextTranslator_ImplementsContract()
    {
        var translator =
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new RecordingHandler(
                        JsonResponse(
                            """
                            {"translations":[]}
                            """
                        )
                    )
                ),
                () => "test-key"
            );

        Assert.IsAssignableFrom<IScreenshotTextTranslator>(
            translator
        );
    }

    private static DeepSeekScreenshotTextTranslator CreateTranslator(
        string translationJson
    )
    {
        return
            new DeepSeekScreenshotTextTranslator(
                new HttpClient(
                    new RecordingHandler(
                        JsonResponse(
                            translationJson
                        )
                    )
                ),
                () => "test-key"
            );
    }

    private static IReadOnlyList<OcrBlock> TwoBlocks()
    {
        return
            new[]
            {
                new OcrBlock(
                    "block-0001",
                    "first",
                    0.90,
                    new PixelRect(
                        10,
                        20,
                        80,
                        30
                    )
                ),
                new OcrBlock(
                    "block-0002",
                    "second",
                    0.80,
                    new PixelRect(
                        100,
                        50,
                        90,
                        30
                    )
                )
            };
    }

    private static HttpResponseMessage JsonResponse(
        string translationJson
    )
    {
        var escaped =
            JsonSerializer.Serialize(
                translationJson
            );

        var responseJson =
            $$"""
            {
              "choices": [
                {
                  "message": {
                    "role": "assistant",
                    "content": {{escaped}}
                  }
                }
              ]
            }
            """;

        return
            new HttpResponseMessage(
                HttpStatusCode.OK
            )
            {
                Content =
                    new StringContent(
                        responseJson,
                        Encoding.UTF8,
                        "application/json"
                    )
            };
    }

    private sealed class RecordingHandler
        : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory;

        public RecordingHandler(
            HttpResponseMessage response
        )
        {
            _responseFactory =
                () => response;
        }

        public string? AuthorizationScheme
        {
            get;
            private set;
        }

        public string? AuthorizationParameter
        {
            get;
            private set;
        }

        public string? RequestBody
        {
            get;
            private set;
        }

        public int SendCount
        {
            get;
            private set;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            SendCount++;

            AuthorizationScheme =
                request.Headers.Authorization?.Scheme;

            AuthorizationParameter =
                request.Headers.Authorization?.Parameter;

            if (request.Content is not null)
            {
                RequestBody =
                    await request.Content.ReadAsStringAsync(
                        cancellationToken
                    );
            }

            return
                _responseFactory();
        }
    }

    private sealed class ThrowingHandler
        : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(
            Exception exception
        )
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return
                Task.FromException<HttpResponseMessage>(
                    _exception
                );
        }
    }

    private sealed class CancellationHandler
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException(
                "The request should have been cancelled."
            );
        }
    }
}
