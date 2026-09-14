namespace LightTranslator.Services.Tray;

public sealed class NotifyIconTrayBackend
    : ITrayIconBackend
{
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;

    private readonly System.Drawing.Icon _icon;

    private readonly System.Windows.Forms.ContextMenuStrip _contextMenu;

    private readonly System.Windows.Forms.ToolStripMenuItem _settingsItem;

    private readonly System.Windows.Forms.ToolStripMenuItem _exitItem;

    private bool _disposed;


    public event Action? SettingsRequested;

    public event Action? ExitRequested;


    public NotifyIconTrayBackend()
    {
        _settingsItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "设置"
            );

        _exitItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "退出"
            );


        _settingsItem.Click +=
            OnSettingsClick;

        _exitItem.Click +=
            OnExitClick;


        _contextMenu =
            new System.Windows.Forms.ContextMenuStrip();

        _contextMenu.Items.Add(
            _settingsItem
        );

        _contextMenu.Items.Add(
            new System.Windows.Forms.ToolStripSeparator()
        );

        _contextMenu.Items.Add(
            _exitItem
        );


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
            new System.Windows.Forms.NotifyIcon
            {
                Text =
                    "LightTranslator",

                Icon =
                    _icon,

                ContextMenuStrip =
                    _contextMenu,

                Visible =
                    false
            };
    }


    public void Show()
    {
        ThrowIfDisposed();

        _notifyIcon.Visible =
            true;
    }


    public void Hide()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible =
            false;
    }


    private void OnSettingsClick(
        object? sender,
        EventArgs e
    )
    {
        SettingsRequested?.Invoke();
    }


    private void OnExitClick(
        object? sender,
        EventArgs e
    )
    {
        ExitRequested?.Invoke();
    }


    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed =
            true;


        _notifyIcon.Visible =
            false;


        _settingsItem.Click -=
            OnSettingsClick;

        _exitItem.Click -=
            OnExitClick;


        _notifyIcon.Dispose();

        _contextMenu.Dispose();

        _settingsItem.Dispose();

        _exitItem.Dispose();

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