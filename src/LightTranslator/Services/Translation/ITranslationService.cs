using LightTranslator.Models;

namespace LightTranslator.Services.Translation;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default
    );
}