namespace LightTranslator.Models;

public enum ScreenshotTextRole
{
    Body,
    Title
}

public sealed record ScreenshotTextRegion(
    string Id,
    string Text,
    double Confidence,
    PixelRect Bounds,
    double SourceLineHeight,
    ScreenshotTextRole Role,
    string? TranslatedText = null
);
