using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class WindowDragGestureHandlerTests
{
    [Fact]
    public void MouseLeftButtonDown_OnNonInteractiveContent_BeginsDrag()
    {
        RunOnSta(
            () =>
            {
                var surface = new Grid();
                var title = new TextBlock();
                var dragCount = 0;

                surface.Children.Add(title);

                using var handler =
                    new WindowDragGestureHandler(
                        surface,
                        () => dragCount++
                    );

                RaiseLeftButtonDown(title);

                Assert.Equal(1, dragCount);
            }
        );
    }

    [Fact]
    public void MouseLeftButtonDown_OnInteractiveControl_DoesNotBeginDrag()
    {
        RunOnSta(
            () =>
            {
                var surface = new Grid();
                var button = new Button();
                var dragCount = 0;

                surface.Children.Add(button);

                using var handler =
                    new WindowDragGestureHandler(
                        surface,
                        () => dragCount++
                    );

                RaiseLeftButtonDown(button);

                Assert.Equal(0, dragCount);
            }
        );
    }

    private static void RaiseLeftButtonDown(
        UIElement source
    )
    {
        source.RaiseEvent(
            new MouseButtonEventArgs(
                Mouse.PrimaryDevice,
                Environment.TickCount,
                MouseButton.Left
            )
            {
                RoutedEvent = Mouse.MouseDownEvent
            }
        );
    }

    private static void RunOnSta(
        Action action
    )
    {
        Exception? exception = null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception caught)
                    {
                        exception = caught;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }
}
