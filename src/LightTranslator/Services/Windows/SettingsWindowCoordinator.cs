using System.Windows;

namespace LightTranslator.Services.Windows;

internal sealed class SettingsWindowCoordinator
{
    private Window? _window;

    private bool _openReserved;

    internal bool TryReserveOpen()
    {
        if (_window is not null)
        {
            RestoreAndActivate(_window);

            return false;
        }

        if (_openReserved)
        {
            return false;
        }

        _openReserved = true;

        return true;
    }

    internal void Show(
        Window window
    )
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!_openReserved)
        {
            throw new InvalidOperationException(
                "A settings window open must be reserved before it is shown."
            );
        }

        if (_window is not null)
        {
            throw new InvalidOperationException(
                "The settings window is already open."
            );
        }

        _window = window;
        window.Closed += OnWindowClosed;

        try
        {
            window.Show();
        }
        catch
        {
            ReleaseWindow(window);

            throw;
        }
        finally
        {
            _openReserved = false;
        }
    }

    internal void CancelOpen()
    {
        _openReserved = false;
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e
    )
    {
        if (sender is Window window)
        {
            ReleaseWindow(window);
        }
    }

    private void ReleaseWindow(
        Window window
    )
    {
        window.Closed -= OnWindowClosed;

        if (ReferenceEquals(_window, window))
        {
            _window = null;
        }
    }

    private static void RestoreAndActivate(
        Window window
    )
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        window.Activate();
        window.Focus();
    }
}
