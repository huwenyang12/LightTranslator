using System.Windows;
using System.Windows.Input;

namespace LightTranslator.Services.Windows;

internal sealed class WindowDragGestureHandler
    : IDisposable
{
    private readonly UIElement _surface;
    private readonly Action _dragMove;
    private readonly MouseButtonEventHandler _mouseDownHandler;

    internal WindowDragGestureHandler(
        UIElement surface,
        Action dragMove
    )
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(dragMove);

        _surface = surface;
        _dragMove = dragMove;
        _mouseDownHandler = OnMouseDown;

        _surface.AddHandler(
            Mouse.MouseDownEvent,
            _mouseDownHandler,
            handledEventsToo: true
        );
    }

    public void Dispose()
    {
        _surface.RemoveHandler(
            Mouse.MouseDownEvent,
            _mouseDownHandler
        );
    }

    private void OnMouseDown(
        object sender,
        MouseButtonEventArgs e
    )
    {
        if (
            e.ChangedButton != MouseButton.Left ||
            e.OriginalSource is not DependencyObject source ||
            !WindowDragHitTest.CanStartDrag(source)
        )
        {
            return;
        }

        _dragMove();
        e.Handled = true;
    }
}
