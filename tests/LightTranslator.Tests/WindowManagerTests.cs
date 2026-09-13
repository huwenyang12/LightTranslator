using LightTranslator.Services.Windows;
using LightTranslator.Views;
namespace LightTranslator.Tests;

public class WindowManagerTests
{

    [Fact]
    public void TranslateWindow_ImplementsManagedWindowContract()
    {
        Assert.True(
            typeof(IManagedWindow).IsAssignableFrom(
                typeof(TranslateWindow)
            )
        );
    }

    [Fact]
    public void ToggleTranslateWindow_FirstCall_ShowsWindow()
    {
        var window = new FakeManagedWindow();

        var manager = new WindowManager(
            () => window
        );

        manager.ToggleTranslateWindow();

        Assert.Equal(
            1,
            window.ShowCount
        );

        Assert.Equal(
            0,
            window.CloseCount
        );
    }

    [Fact]
    public void ToggleTranslateWindow_SecondCall_ClosesWindow()
    {
        var window = new FakeManagedWindow();

        var manager = new WindowManager(
            () => window
        );

        manager.ToggleTranslateWindow();
        manager.ToggleTranslateWindow();

        Assert.Equal(
            1,
            window.ShowCount
        );

        Assert.Equal(
            1,
            window.CloseCount
        );
    }

    private sealed class FakeManagedWindow
        : IManagedWindow
    {
        public int ShowCount { get; private set; }

        public int CloseCount { get; private set; }

        public event EventHandler? Closed;

        public void Show()
        {
            ShowCount++;
        }


        public void Close()
        {
            CloseCount++;

            Closed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }
}