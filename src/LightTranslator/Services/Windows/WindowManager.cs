namespace LightTranslator.Services.Windows;

public sealed class WindowManager
{
    private const double MinimumVisibleWidth = 80;

    private const double MinimumVisibleHeight = 36;

    private readonly Func<IManagedWindow> _translateWindowFactory;

    private IManagedWindow? _translateWindow;

    private WindowPlacement? _lastPlacement;

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

        if (
            _lastPlacement is { } placement &&
            placement.IsVisibleWithin(
                GetVirtualScreenBounds()
            )
        )
        {
            placement.ApplyTo(_translateWindow);
        }
        else
        {
            _lastPlacement = null;
        }

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
            _lastPlacement =
                WindowPlacement.From(
                    _translateWindow
                );

            _translateWindow.Closed -=
                OnTranslateWindowClosed;
        }

        _translateWindow = null;
    }

    private readonly record struct WindowPlacement(
        double Left,
        double Top,
        double Width,
        double Height
    )
    {
        internal static WindowPlacement From(
            IManagedWindow window
        ) =>
            new(
                window.Left,
                window.Top,
                window.Width,
                window.Height
            );

        internal void ApplyTo(
            IManagedWindow window
        )
        {
            window.WindowStartupLocation =
                System.Windows.WindowStartupLocation.Manual;

            window.Left = Left;
            window.Top = Top;
            window.Width = Width;
            window.Height = Height;
        }

        internal bool IsVisibleWithin(
            System.Windows.Rect desktopBounds
        )
        {
            if (
                !double.IsFinite(Left) ||
                !double.IsFinite(Top) ||
                !double.IsFinite(Width) ||
                !double.IsFinite(Height) ||
                Width <= 0 ||
                Height <= 0 ||
                desktopBounds.IsEmpty
            )
            {
                return false;
            }

            var visibleBounds =
                System.Windows.Rect.Intersect(
                    new System.Windows.Rect(
                        Left,
                        Top,
                        Width,
                        Height
                    ),
                    desktopBounds
                );

            return
                !visibleBounds.IsEmpty &&
                visibleBounds.Width >=
                    Math.Min(
                        Width,
                        MinimumVisibleWidth
                    ) &&
                visibleBounds.Height >=
                    Math.Min(
                        Height,
                        MinimumVisibleHeight
                    );
        }
    }

    private static System.Windows.Rect GetVirtualScreenBounds() =>
        new(
            System.Windows.SystemParameters.VirtualScreenLeft,
            System.Windows.SystemParameters.VirtualScreenTop,
            System.Windows.SystemParameters.VirtualScreenWidth,
            System.Windows.SystemParameters.VirtualScreenHeight
        );
}
