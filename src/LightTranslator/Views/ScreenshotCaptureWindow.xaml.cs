using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LightTranslator.Models;
using LightTranslator.Services.ScreenCapture;

using WpfKeyEventArgs =
    System.Windows.Input.KeyEventArgs;

using WpfMouseEventArgs =
    System.Windows.Input.MouseEventArgs;

using WpfPoint =
    System.Windows.Point;

namespace LightTranslator.Views;

public partial class ScreenshotCaptureWindow
    : Window
{
    private readonly ScreenCaptureFrame _frame;

    private WpfPoint? _startPoint;

    public ScreenshotCaptureWindow(
        ScreenCaptureFrame frame
    )
    {
        _frame =
            frame ??
            throw new ArgumentNullException(
                nameof(
                    frame
                )
            );

        InitializeComponent();

        FrozenScreenImage.Source =
            frame.Image;

        var windowBounds =
            DpiCoordinateMapper.PixelsToDips(
                frame.MonitorBounds,
                frame.DpiX,
                frame.DpiY
            );

        Left =
            windowBounds.X;

        Top =
            windowBounds.Y;

        Width =
            windowBounds.Width;

        Height =
            windowBounds.Height;

        PreviewKeyDown +=
            OnPreviewKeyDown;
    }

    public PixelRect? Selection
    {
        get;
        private set;
    }

    internal PixelRect ConvertSelectionToPixels(
        Rect selectionBounds
    )
    {
        return
            DpiCoordinateMapper.DipsToPixels(
                selectionBounds,
                _frame.DpiX,
                _frame.DpiY
            );
    }

    internal static string FormatSelectionDimensions(
        PixelRect selection
    )
    {
        return $"{selection.Width} × {selection.Height}";
    }

    private void OnMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e
    )
    {
        _startPoint =
            e.GetPosition(
                CaptureCanvas
            );

        CaptureCanvas.CaptureMouse();

        SelectionRectangle.Visibility =
            Visibility.Visible;

        SelectionDimensionPill.Visibility =
            Visibility.Visible;

        Canvas.SetLeft(
            SelectionRectangle,
            _startPoint.Value.X
        );

        Canvas.SetTop(
            SelectionRectangle,
            _startPoint.Value.Y
        );

        SelectionRectangle.Width =
            0;

        SelectionRectangle.Height =
            0;
    }

    private void OnMouseMove(
        object sender,
        WpfMouseEventArgs e
    )
    {
        if (_startPoint is null)
        {
            return;
        }

        UpdateSelectionRectangle(
            e.GetPosition(
                CaptureCanvas
            )
        );
    }

    private void OnMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e
    )
    {
        if (_startPoint is null)
        {
            return;
        }

        var endPoint =
            e.GetPosition(
                CaptureCanvas
            );

        UpdateSelectionRectangle(
            endPoint
        );

        var selectionBounds =
            CreateSelectionBounds(
                _startPoint.Value,
                endPoint
            );

        _startPoint =
            null;

        CaptureCanvas.ReleaseMouseCapture();

        var selection =
            ConvertSelectionToPixels(
                selectionBounds
            );

        if (selection.IsEmpty)
        {
            ResetSelectionFeedback();

            return;
        }

        Selection =
            selection;

        DialogResult =
            true;
    }

    private void UpdateSelectionRectangle(
        WpfPoint current
    )
    {
        if (_startPoint is null)
        {
            return;
        }

        var bounds =
            CreateSelectionBounds(
                _startPoint.Value,
                current
            );

        Canvas.SetLeft(
            SelectionRectangle,
            bounds.X
        );

        Canvas.SetTop(
            SelectionRectangle,
            bounds.Y
        );

        SelectionRectangle.Width =
            bounds.Width;

        SelectionRectangle.Height =
            bounds.Height;

        UpdateSelectionDimensionPill(
            bounds
        );
    }

    private void UpdateSelectionDimensionPill(
        Rect selectionBounds
    )
    {
        var selection =
            ConvertSelectionToPixels(
                selectionBounds
            );

        SelectionDimensionTextBlock.Text =
            FormatSelectionDimensions(
                selection
            );

        SelectionDimensionPill.Measure(
            new System.Windows.Size(
                double.PositiveInfinity,
                double.PositiveInfinity
            )
        );

        var pillSize =
            SelectionDimensionPill.DesiredSize;

        var left =
            Math.Max(
                0d,
                selectionBounds.X +
                (selectionBounds.Width - pillSize.Width) / 2d
            );

        left =
            Math.Min(
                left,
                Math.Max(
                    0d,
                    CaptureCanvas.ActualWidth - pillSize.Width
                )
            );

        var belowTop =
            selectionBounds.Bottom +
            8d;

        var top =
            belowTop + pillSize.Height <=
            CaptureCanvas.ActualHeight
                ? belowTop
                : Math.Max(
                    0d,
                    selectionBounds.Top -
                    pillSize.Height -
                    8d
                );

        Canvas.SetLeft(
            SelectionDimensionPill,
            left
        );

        Canvas.SetTop(
            SelectionDimensionPill,
            top
        );
    }

    internal void ResetSelectionFeedback()
    {
        SelectionRectangle.Visibility =
            Visibility.Collapsed;

        SelectionDimensionPill.Visibility =
            Visibility.Collapsed;
    }

    private static Rect CreateSelectionBounds(
        WpfPoint start,
        WpfPoint end
    )
    {
        return
            new Rect(
                Math.Min(
                    start.X,
                    end.X
                ),
                Math.Min(
                    start.Y,
                    end.Y
                ),
                Math.Abs(
                    end.X -
                    start.X
                ),
                Math.Abs(
                    end.Y -
                    start.Y
                )
            );
    }

    private void OnPreviewKeyDown(
        object sender,
        WpfKeyEventArgs e
    )
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Selection =
            null;

        ResetSelectionFeedback();

        DialogResult =
            false;
    }
}
