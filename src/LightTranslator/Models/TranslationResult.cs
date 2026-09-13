namespace LightTranslator.Models;

public sealed record TranslationResult(
    string Text,
    string? DetectedSourceLanguage
);