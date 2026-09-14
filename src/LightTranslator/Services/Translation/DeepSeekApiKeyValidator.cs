using System.Net.Http;
using LightTranslator.Models;

namespace LightTranslator.Services.Translation;

public sealed class DeepSeekApiKeyValidator
    : IApiKeyValidator
{
    private readonly HttpClient _httpClient;

    public DeepSeekApiKeyValidator(
        HttpClient httpClient
    )
    {
        _httpClient =
            httpClient;
    }

    public async Task ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default
    )
    {
        var translationService =
            new DeepSeekTranslationService(
                _httpClient,
                () => apiKey
            );

        await translationService.TranslateAsync(
            new TranslationRequest(
                "test",
                "en",
                "zh"
            ),
            cancellationToken
        );
    }
}