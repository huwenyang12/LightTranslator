using System.Net;
using System.Text;
using LightTranslator.Models;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public class DeepSeekTranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_ParsesTranslationAndDetectedLanguage()
    {
        var responseJson = """
        {
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": "{\"translated_text\":\"Hello\",\"detected_source_language\":\"zh\"}"
              }
            }
          ]
        }
        """;

        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            responseJson
        );

        var service = new DeepSeekTranslationService(
            new HttpClient(handler),
            () => "test-api-key"
        );

        var result = await service.TranslateAsync(
            new TranslationRequest(
                "你好",
                "auto",
                "en"
            )
        );

        Assert.Equal(
            "Hello",
            result.Text
        );

        Assert.Equal(
            "zh",
            result.DetectedSourceLanguage
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenApiKeyMissing_ThrowsConfigurationError()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.OK,
                    "{}"
                )
            ),
            () => null
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.Configuration,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenUnauthorized_MapsToInvalidApiKey()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.Unauthorized,
                    "{}"
                )
            ),
            () => "invalid-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.InvalidApiKey,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenBalanceIsInsufficient_MapsCorrectly()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.PaymentRequired,
                    "{}"
                )
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.InsufficientBalance,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenRateLimited_MapsCorrectly()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.TooManyRequests,
                    "{}"
                )
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.RateLimited,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_SendsExpectedDeepSeekRequest()
    {
        var responseJson = """
        {
        "choices": [
            {
            "message": {
                "role": "assistant",
                "content": "{\"translated_text\":\"Hello\",\"detected_source_language\":\"zh\"}"
            }
            }
        ]
        }
        """;

        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            responseJson
        );

        var service = new DeepSeekTranslationService(
            new HttpClient(handler),
            () => "test-api-key"
        );

        await service.TranslateAsync(
            new TranslationRequest(
                "你好",
                "auto",
                "en"
            )
        );

        Assert.Equal(
            "Bearer",
            handler.AuthorizationScheme
        );

        Assert.Equal(
            "test-api-key",
            handler.AuthorizationParameter
        );

        Assert.NotNull(handler.RequestBody);

        using var document =
            System.Text.Json.JsonDocument.Parse(
                handler.RequestBody!
            );

        var root = document.RootElement;

        Assert.Equal(
            "deepseek-v4-flash",
            root.GetProperty("model").GetString()
        );

        Assert.Equal(
            "disabled",
            root
                .GetProperty("thinking")
                .GetProperty("type")
                .GetString()
        );

        Assert.Equal(
            "json_object",
            root
                .GetProperty("response_format")
                .GetProperty("type")
                .GetString()
        );

        Assert.False(
            root.GetProperty("stream").GetBoolean()
        );

        var messages = root.GetProperty("messages");

        Assert.Equal(
            "system",
            messages[0]
                .GetProperty("role")
                .GetString()
        );

        Assert.Contains(
            "JSON",
            messages[0]
                .GetProperty("content")
                .GetString()
        );

        Assert.Equal(
            "user",
            messages[1]
                .GetProperty("role")
                .GetString()
        );

        Assert.Equal(
            "你好",
            messages[1]
                .GetProperty("content")
                .GetString()
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenResponseIsMalformed_MapsToInvalidResponse()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.OK,
                    "{}"
                )
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.InvalidResponse,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenContentIsNotJson_MapsToInvalidResponse()
    {
        var responseJson = """
        {
        "choices": [
            {
            "message": {
                "role": "assistant",
                "content": "this is not json"
            }
            }
        ]
        }
        """;

        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.OK,
                    responseJson
                )
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.InvalidResponse,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenInputIsBlank_ReturnsEmptyWithoutHttpRequest()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            "{}"
        );

        var service = new DeepSeekTranslationService(
            new HttpClient(handler),
            () => "test-key"
        );

        var result = await service.TranslateAsync(
            new TranslationRequest(
                "   ",
                "auto",
                "en"
            )
        );

        Assert.Equal(
            string.Empty,
            result.Text
        );

        Assert.Null(
            result.DetectedSourceLanguage
        );

        Assert.Equal(
            0,
            handler.SendCount
        );
    }


    [Fact]
    public async Task TranslateAsync_WhenRequestTimesOut_MapsToTimeout()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new TimeoutHttpMessageHandler()
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.Timeout,
            exception.Kind
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var service = new DeepSeekTranslationService(
            new HttpClient(
                new CancellationHttpMessageHandler()
            ),
            () => "test-key"
        );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.TranslateAsync(
                new TranslationRequest(
                    "你好",
                    "auto",
                    "en"
                ),
                cancellationTokenSource.Token
            )
        );
    }

    [Fact]
    public async Task TranslateAsync_WhenNetworkFails_MapsToNetwork()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new NetworkFailureHttpMessageHandler()
            ),
            () => "test-key"
        );

        var exception =
            await Assert.ThrowsAsync<TranslationException>(
                () => service.TranslateAsync(
                    new TranslationRequest(
                        "你好",
                        "auto",
                        "en"
                    )
                )
            );

        Assert.Equal(
            TranslationErrorKind.Network,
            exception.Kind
        );
    }

    [Fact]
    public void DeepSeekTranslationService_ImplementsTranslationServiceContract()
    {
        var service = new DeepSeekTranslationService(
            new HttpClient(
                new StubHttpMessageHandler(
                    HttpStatusCode.OK,
                    "{}"
                )
            ),
            () => "test-key"
        );

        Assert.IsAssignableFrom<ITranslationService>(
            service
        );
    }

    private sealed class StubHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseJson;

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string? RequestBody { get; private set; }

        public int SendCount { get; private set; }

        public StubHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseJson
        )
        {
            _statusCode = statusCode;
            _responseJson = responseJson;
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

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseJson,
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }
    }

    private sealed class TimeoutHttpMessageHandler
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new TaskCanceledException(
                "Simulated timeout."
            );
        }
    }

    private sealed class CancellationHttpMessageHandler
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
    private sealed class NetworkFailureHttpMessageHandler
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new HttpRequestException(
                "Simulated network failure."
            );
        }
    }
}