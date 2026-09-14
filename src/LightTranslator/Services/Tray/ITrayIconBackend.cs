namespace LightTranslator.Services.Tray;

public interface ITrayIconBackend
    : IDisposable
{
    event Action? SettingsRequested;

    event Action? ExitRequested;

    event Action? TextTranslationRequested;

    event Action? ScreenshotTranslationRequested;

    void Show();

    void Hide();
}