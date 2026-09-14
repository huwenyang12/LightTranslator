namespace LightTranslator.Services.Translation;

public interface IApiKeyValidator
{
    Task ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default
    );
}