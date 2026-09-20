using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace LightTranslator.Services.Tray;

public sealed class NotifyIconTrayBackend
    : ITrayIconBackend
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly System.Drawing.Icon _icon;
    private readonly Func<ITrayMenu> _menuFactory;
    private readonly Dispatcher _dispatcher;

    private ITrayMenu? _activeMenu;
    private bool _disposed;

    public event Action? SettingsRequested;

    public event Action? ExitRequested;

    public event Action? TextTranslationRequested;

    public event Action? ScreenshotTranslationRequested;

    public NotifyIconTrayBackend()
        : this(
            () => new TrayMenuWindow(),
            System.Windows.Application.Current?.Dispatcher
            ?? Dispatcher.CurrentDispatcher
        )
    {
    }

    internal NotifyIconTrayBackend(
        Func<ITrayMenu> menuFactory,
        Dispatcher dispatcher
    )
    {
        ArgumentNullException.ThrowIfNull(menuFactory);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _menuFactory = menuFactory;
        _dispatcher = dispatcher;

        var executablePath =
            Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "Unable to determine application executable path."
            );

        _icon =
            System.Drawing.Icon.ExtractAssociatedIcon(
                executablePath
            )
            ?? (System.Drawing.Icon)
                System.Drawing.SystemIcons.Application.Clone();

        _notifyIcon =
            new Forms.NotifyIcon
            {
                Text = "语桥",
                Icon = _icon,
                ContextMenuStrip = null,
                Visible = false
            };

        _notifyIcon.MouseUp += OnNotifyIconMouseUp;
    }

    public void Show()
    {
        ThrowIfDisposed();
        _notifyIcon.Visible = true;
    }

    public void Hide()
    {
        if (_disposed)
        {
            return;
        }

        CloseActiveMenu();
        _notifyIcon.Visible = false;
    }

    private void OnNotifyIconMouseUp(
        object? sender,
        Forms.MouseEventArgs e
    )
    {
        if (e.Button != Forms.MouseButtons.Right)
        {
            return;
        }

        _dispatcher.Invoke(ShowTrayMenu);
    }

    internal void ShowTrayMenu()
    {
        ThrowIfDisposed();

        if (_activeMenu?.IsVisible == true)
        {
            return;
        }

        CloseActiveMenu();

        var menu = _menuFactory();

        menu.TextTranslationRequested +=
            OnTextTranslationRequested;
        menu.ScreenshotTranslationRequested +=
            OnScreenshotTranslationRequested;
        menu.SettingsRequested +=
            OnSettingsRequested;
        menu.ExitRequested +=
            OnExitRequested;
        menu.Closed += OnMenuClosed;

        _activeMenu = menu;
        menu.ShowAtCursor();
    }

    private void OnTextTranslationRequested()
    {
        CloseActiveMenu();
        TextTranslationRequested?.Invoke();
    }

    private void OnScreenshotTranslationRequested()
    {
        CloseActiveMenu();
        ScreenshotTranslationRequested?.Invoke();
    }

    private void OnSettingsRequested()
    {
        CloseActiveMenu();
        SettingsRequested?.Invoke();
    }

    private void OnExitRequested()
    {
        CloseActiveMenu();
        ExitRequested?.Invoke();
    }

    private void OnMenuClosed(
        object? sender,
        EventArgs e
    )
    {
        if (sender is not ITrayMenu menu)
        {
            return;
        }

        DetachMenu(menu);

        if (ReferenceEquals(_activeMenu, menu))
        {
            _activeMenu = null;
        }
    }

    private void CloseActiveMenu()
    {
        var menu = _activeMenu;

        if (menu is null)
        {
            return;
        }

        _activeMenu = null;
        DetachMenu(menu);

        if (menu.IsVisible)
        {
            menu.Close();
        }
    }

    private void DetachMenu(
        ITrayMenu menu
    )
    {
        menu.TextTranslationRequested -=
            OnTextTranslationRequested;
        menu.ScreenshotTranslationRequested -=
            OnScreenshotTranslationRequested;
        menu.SettingsRequested -=
            OnSettingsRequested;
        menu.ExitRequested -=
            OnExitRequested;
        menu.Closed -= OnMenuClosed;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.MouseUp -= OnNotifyIconMouseUp;

        if (_dispatcher.CheckAccess())
        {
            CloseActiveMenu();
        }
        else
        {
            _dispatcher.Invoke(CloseActiveMenu);
        }

        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(NotifyIconTrayBackend)
            );
        }
    }
}
