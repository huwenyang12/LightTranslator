using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using LightTranslator.Models;

using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace LightTranslator.Views;

public partial class ScreenshotCaptureWindow
    : Window
{

    private WpfPoint? _startPoint;


    public ScreenshotSelection? Selection
    {
        get;
        private set;
    }


    public ScreenshotCaptureWindow()
    {
        InitializeComponent();

        PreviewKeyDown +=
            OnPreviewKeyDown;
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

        SelectionRectangle.Visibility =
            Visibility.Visible;

        Canvas.SetLeft(
            SelectionRectangle,
            _startPoint.Value.X
        );

        Canvas.SetTop(
            SelectionRectangle,
            _startPoint.Value.Y
        );
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


        var current =
            e.GetPosition(
                CaptureCanvas
            );


        var x =
            Math.Min(
                _startPoint.Value.X,
                current.X
            );

        var y =
            Math.Min(
                _startPoint.Value.Y,
                current.Y
            );


        var width =
            Math.Abs(
                current.X -
                _startPoint.Value.X
            );

        var height =
            Math.Abs(
                current.Y -
                _startPoint.Value.Y
            );


        Canvas.SetLeft(
            SelectionRectangle,
            x
        );

        Canvas.SetTop(
            SelectionRectangle,
            y
        );


        SelectionRectangle.Width =
            width;

        SelectionRectangle.Height =
            height;
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


        var end =
            e.GetPosition(
                CaptureCanvas
            );


        Selection =
            new ScreenshotSelection(
                Math.Min(
                    _startPoint.Value.X,
                    end.X
                ),
                Math.Min(
                    _startPoint.Value.Y,
                    end.Y
                ),
                Math.Abs(
                    end.X -
                    _startPoint.Value.X
                ),
                Math.Abs(
                    end.Y -
                    _startPoint.Value.Y
                )
            );


        DialogResult =
            true;
    }


    private void OnPreviewKeyDown(
        object sender,
        WpfKeyEventArgs e
    )
    {
        if (e.Key == Key.Escape)
        {
            DialogResult =
                false;
        }
    }
}