namespace LightTranslator.Services.Windows;

public sealed class WindowManager
{
    private readonly Func<IManagedWindow> _translateWindowFactory;

    private IManagedWindow? _translateWindow;

    public WindowManager(
        Func<IManagedWindow> translateWindowFactory
    )
    {
        _translateWindowFactory =
            translateWindowFactory;
    }

    public void ToggleTranslateWindow()
    {
        if (_translateWindow is not null)
        {
            _translateWindow.Close();

            return;
        }

        _translateWindow =
            _translateWindowFactory();

        _translateWindow.Closed +=
            OnTranslateWindowClosed;

        _translateWindow.Show();
    }

    private void OnTranslateWindowClosed(
        object? sender,
        EventArgs e
    )
    {
        if (_translateWindow is not null)
        {
            _translateWindow.Closed -=
                OnTranslateWindowClosed;
        }

        _translateWindow = null;
    }
}