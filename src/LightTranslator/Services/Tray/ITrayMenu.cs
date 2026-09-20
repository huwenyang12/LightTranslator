namespace LightTranslator.Services.Tray;

internal interface ITrayMenu
{
    event Action? TextTranslationRequested;

    event Action? ScreenshotTranslationRequested;

    event Action? SettingsRequested;

    event Action? ExitRequested;

    event EventHandler? Closed;

    bool IsVisible { get; }

    void ShowAtCursor();

    void Close();
}
