namespace LightTranslator.Services.Tray;

public interface ITrayIconBackend
    : IDisposable
{
    event Action? SettingsRequested;

    event Action? ExitRequested;

    void Show();

    void Hide();
}