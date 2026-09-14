using System.Net;
using LightTranslator.Services.Translation;

namespace LightTranslator.Tests;

public sealed class DeepSeekApiKeyValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WhenDeepSeekAcceptsKey_CompletesSuccessfully()
    {
        var handler =
            new StubHttpMessageHandler(
                new HttpResponseMessage(
                    HttpStatusCode.OK
                )
                {
                    Content =
                        new StringContent(
                            """
                            {
                              "choices": [
                                {
                                  "message": {
                                    "content": "{\"translated_text\":\"测试\",\"detected_source_language\":\"en\"}"
                                  }
                                }
                              ]
                            }
                            """
                        )
                }
            );

        using var httpClient =
            new HttpClient(
                handler
            );

        var validator =
            new DeepSeekApiKeyValidator(
                httpClient
            );

        await validator.ValidateAsync(
            "test-key"
        );

        Assert.Equal(
            "Bearer",
            handler.AuthorizationScheme
        );

        Assert.Equal(
            "test-key",
            handler.AuthorizationParameter
        );
    }

    private sealed class StubHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(
            HttpResponseMessage response
        )
        {
            _response =
                response;
        }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            AuthorizationScheme =
                request.Headers.Authorization?.Scheme;

            AuthorizationParameter =
                request.Headers.Authorization?.Parameter;

            return Task.FromResult(
                _response
            );
        }
    }
}