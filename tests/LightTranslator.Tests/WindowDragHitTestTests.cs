using System.Windows.Controls;
using System.Windows.Documents;
using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class WindowDragHitTestTests
{
    [Fact]
    public void CanStartDrag_AllowsNonInteractiveSurface()
    {
        RunOnSta(
            () => Assert.True(CanStartDrag(new Border()))
        );
    }

    [Fact]
    public void CanStartDrag_AllowsSurfaceAttachedToWindow()
    {
        RunOnSta(
            () =>
            {
                var surface = new Border();
                var window = new System.Windows.Window
                {
                    Content = surface
                };

                Assert.True(CanStartDrag(surface));

                window.Close();
            }
        );
    }

    [Theory]
    [InlineData(typeof(Button))]
    [InlineData(typeof(TextBox))]
    [InlineData(typeof(ComboBox))]
    public void CanStartDrag_RejectsInteractiveControls(
        Type controlType
    )
    {
        RunOnSta(
            () =>
            {
                var control =
                    Assert.IsAssignableFrom<Control>(
                        Activator.CreateInstance(controlType)
                    );

                Assert.False(CanStartDrag(control));
            }
        );
    }

    [Fact]
    public void CanStartDrag_RejectsTextContentInsideInteractiveControl()
    {
        RunOnSta(
            () =>
            {
                var run = new Run("复制");
                var text = new TextBlock();

                text.Inlines.Add(run);

                var button = new Button
                {
                    Content = text
                };

                Assert.False(CanStartDrag(run));
            }
        );
    }

    private static bool CanStartDrag(
        System.Windows.DependencyObject source
    )
    {
        var type =
            typeof(WindowBackdropService).Assembly.GetType(
                "LightTranslator.Services.Windows.WindowDragHitTest"
            );

        Assert.NotNull(type);
        Assert.True(type.IsNotPublic);

        var method =
            type.GetMethod(
                "CanStartDrag",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic
            );

        Assert.NotNull(method);

        return Assert.IsType<bool>(
            method.Invoke(
                null,
                [source]
            )
        );
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(
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

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }
}
