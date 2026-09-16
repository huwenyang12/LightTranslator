namespace LightTranslator.Models;

public sealed record AppSettings
{
    public bool FirstRunCompleted { get; init; }

    public string TextSourceLanguage { get; init; } = "auto";

    public string TextTargetLanguage { get; init; } = "zh";

    public string ScreenshotSourceLanguage { get; init; } = "auto";

    public string ScreenshotTargetLanguage { get; init; } = "zh";

    public HotkeyDefinition? TextTranslationHotkey { get; init; }

    public HotkeyDefinition? ScreenshotTranslationHotkey { get; init; }

    public bool StartWithWindows { get; init; }

    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }
}