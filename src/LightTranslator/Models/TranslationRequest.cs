namespace LightTranslator.Models;

public sealed record TranslationRequest(
    string Text,
    string SourceLanguage,
    string TargetLanguage
);