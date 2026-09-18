namespace LightTranslator.Services.Tray;

public sealed class NotifyIconTrayBackend
    : ITrayIconBackend
{
    private readonly System.Windows.Forms.NotifyIcon _notifyIcon;

    private readonly System.Drawing.Icon _icon;

    private readonly System.Windows.Forms.ContextMenuStrip _contextMenu;

    private readonly System.Windows.Forms.ToolStripMenuItem
        _textTranslationItem;

    private readonly System.Windows.Forms.ToolStripMenuItem
        _screenshotTranslationItem;

    private readonly System.Windows.Forms.ToolStripMenuItem
        _settingsItem;

    private readonly System.Windows.Forms.ToolStripMenuItem
        _exitItem;

    private bool _disposed;

    public event Action? SettingsRequested;

    public event Action? ExitRequested;

    public event Action? TextTranslationRequested;

    public event Action? ScreenshotTranslationRequested;

    public NotifyIconTrayBackend()
    {
        _textTranslationItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "文本翻译"
            );

        _screenshotTranslationItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "截图翻译"
            );

        _settingsItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "设置"
            );

        _exitItem =
            new System.Windows.Forms.ToolStripMenuItem(
                "退出"
            );

        _textTranslationItem.Click +=
            OnTextTranslationClick;

        _screenshotTranslationItem.Click +=
            OnScreenshotTranslationClick;

        _settingsItem.Click +=
            OnSettingsClick;

        _exitItem.Click +=
            OnExitClick;

        _contextMenu =
            new System.Windows.Forms.ContextMenuStrip
            {
                ShowImageMargin = false,
                Font = System.Drawing.SystemFonts.MenuFont,
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.FromArgb(
                    29,
                    29,
                    31
                ),
                Renderer = new System.Windows.Forms.ToolStripProfessionalRenderer(
                    new BridgoMenuColorTable()
                )
            };

        ConfigureMenuItem(_textTranslationItem);
        ConfigureMenuItem(_screenshotTranslationItem);
        ConfigureMenuItem(_settingsItem);
        ConfigureMenuItem(_exitItem);

        _contextMenu.Items.Add(
            _textTranslationItem
        );

        _contextMenu.Items.Add(
            _screenshotTranslationItem
        );

        _contextMenu.Items.Add(
            new System.Windows.Forms.ToolStripSeparator()
        );

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
                    "语桥",

                Icon =
                    _icon,

                ContextMenuStrip =
                    _contextMenu,

                Visible =
                    false
            };
    }

    private static void ConfigureMenuItem(
        System.Windows.Forms.ToolStripMenuItem item
    )
    {
        item.AutoSize = true;
        item.Padding = new System.Windows.Forms.Padding(
            10,
            5,
            10,
            5
        );
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

    private void OnTextTranslationClick(
        object? sender,
        EventArgs e
    )
    {
        TextTranslationRequested?.Invoke();
    }

    private void OnScreenshotTranslationClick(
        object? sender,
        EventArgs e
    )
    {
        ScreenshotTranslationRequested?.Invoke();
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

        _textTranslationItem.Click -=
            OnTextTranslationClick;

        _screenshotTranslationItem.Click -=
            OnScreenshotTranslationClick;

        _settingsItem.Click -=
            OnSettingsClick;

        _exitItem.Click -=
            OnExitClick;

        _notifyIcon.Dispose();

        _contextMenu.Dispose();

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

    private sealed class BridgoMenuColorTable
        : System.Windows.Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color ToolStripDropDownBackground =>
            System.Drawing.Color.White;

        public override System.Drawing.Color MenuItemSelected =>
            System.Drawing.Color.FromArgb(232, 240, 254);

        public override System.Drawing.Color MenuItemBorder =>
            System.Drawing.Color.Transparent;

        public override System.Drawing.Color SeparatorDark =>
            System.Drawing.Color.FromArgb(226, 226, 230);

        public override System.Drawing.Color SeparatorLight =>
            System.Drawing.Color.White;

        public override System.Drawing.Color ImageMarginGradientBegin =>
            System.Drawing.Color.White;

        public override System.Drawing.Color ImageMarginGradientMiddle =>
            System.Drawing.Color.White;

        public override System.Drawing.Color ImageMarginGradientEnd =>
            System.Drawing.Color.White;
    }
}
