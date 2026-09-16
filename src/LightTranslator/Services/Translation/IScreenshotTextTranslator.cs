using LightTranslator.Models;

namespace LightTranslator.Services.Translation;

public interface IScreenshotTextTranslator
{
    Task<IReadOnlyDictionary<string, string>> TranslateAsync(
        IReadOnlyList<OcrBlock> blocks,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    );
}
