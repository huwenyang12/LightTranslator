namespace LightTranslator.Services.Tray;

public sealed class TrayService
    : IDisposable
{
    private readonly ITrayIconBackend _backend;

    public event Action? SettingsRequested;

    public event Action? ExitRequested;

    public TrayService(
        ITrayIconBackend backend
    )
    {
        _backend =
            backend;

        _backend.SettingsRequested +=
            OnSettingsRequested;

        _backend.ExitRequested +=
            OnExitRequested;
    }

    public void Start()
    {
        _backend.Show();
    }

    public void Stop()
    {
        _backend.Hide();
    }

    private void OnSettingsRequested()
    {
        SettingsRequested?.Invoke();
    }

    private void OnExitRequested()
    {
        ExitRequested?.Invoke();
    }

    public void Dispose()
    {
        _backend.SettingsRequested -=
            OnSettingsRequested;

        _backend.ExitRequested -=
            OnExitRequested;

        _backend.Hide();

        _backend.Dispose();
    }
}