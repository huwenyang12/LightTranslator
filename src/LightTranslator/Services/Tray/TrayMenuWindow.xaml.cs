using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace LightTranslator.Services.Tray;

public partial class TrayMenuWindow
    : Window, ITrayMenu
{
    public event Action? TextTranslationRequested;

    public event Action? ScreenshotTranslationRequested;

    public event Action? SettingsRequested;

    public event Action? ExitRequested;

    public TrayMenuWindow()
    {
        InitializeComponent();
    }

    public void ShowAtCursor()
    {
        if (IsVisible)
        {
            Activate();
            return;
        }

        Show();
        UpdateLayout();

        PositionNearCursor();

        Opacity = 1;
        Activate();
        TextTranslationButton.Focus();
    }

    private void PositionNearCursor()
    {
        var cursor = Forms.Cursor.Position;
        var screen = Forms.Screen.FromPoint(cursor);
        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY;
        var workArea = screen.WorkingArea;
        var margin = 8d;
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight;
        var cursorX = cursor.X / scaleX;
        var cursorY = cursor.Y / scaleY;
        var workLeft = workArea.Left / scaleX;
        var workTop = workArea.Top / scaleY;
        var workWidth = workArea.Width / scaleX;
        var workHeight = workArea.Height / scaleY;
        var position = TrayMenuPlacement.Calculate(
            cursorX,
            cursorY,
            width,
            height,
            new Rect(
                workLeft,
                workTop,
                workWidth,
                workHeight
            ),
            margin
        );

        Left = position.X;
        Top = position.Y;
    }

    private void OnTextTranslationClick(
        object sender,
        RoutedEventArgs e
    )
    {
        TextTranslationRequested?.Invoke();
    }

    private void OnScreenshotTranslationClick(
        object sender,
        RoutedEventArgs e
    )
    {
        ScreenshotTranslationRequested?.Invoke();
    }

    private void OnSettingsClick(
        object sender,
        RoutedEventArgs e
    )
    {
        SettingsRequested?.Invoke();
    }

    private void OnExitClick(
        object sender,
        RoutedEventArgs e
    )
    {
        ExitRequested?.Invoke();
    }

    private void OnDeactivated(
        object? sender,
        EventArgs e
    )
    {
        Close();
    }

    private void OnPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        Close();
    }
}
