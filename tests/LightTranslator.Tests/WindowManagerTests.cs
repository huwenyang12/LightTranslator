using LightTranslator.Services.Windows;
using LightTranslator.Views;
using System.Windows;

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

    [Fact]
    public void ToggleTranslateWindow_WhenMinimized_RestoresAndActivatesWindow()
    {
        var window = new FakeManagedWindow();

        var manager = new WindowManager(
            () => window
        );

        manager.ToggleTranslateWindow();
        window.WindowState = WindowState.Minimized;

        manager.ToggleTranslateWindow();

        Assert.Equal(WindowState.Normal, window.WindowState);
        Assert.Equal(1, window.ActivateCount);
        Assert.Equal(0, window.CloseCount);
    }

    [Fact]
    public void ToggleTranslateWindow_AfterClose_RestoresLastSessionPlacement()
    {
        var firstWindow =
            new FakeManagedWindow
            {
                Left = 120,
                Top = 80,
                Width = 680,
                Height = 420
            };
        var secondWindow =
            new FakeManagedWindow();
        var windows =
            new Queue<IManagedWindow>(
                [firstWindow, secondWindow]
            );
        var manager =
            new WindowManager(
                () => windows.Dequeue()
            );

        manager.ToggleTranslateWindow();
        manager.ToggleTranslateWindow();
        manager.ToggleTranslateWindow();

        Assert.Equal(120, secondWindow.Left);
        Assert.Equal(80, secondWindow.Top);
        Assert.Equal(680, secondWindow.Width);
        Assert.Equal(420, secondWindow.Height);
        Assert.Equal(
            WindowStartupLocation.Manual,
            secondWindow.WindowStartupLocation
        );
        Assert.Equal(1, secondWindow.ShowCount);
    }

    [Fact]
    public void ToggleTranslateWindow_WhenLastPositionIsOffScreen_UsesDefaults()
    {
        var firstWindow =
            new FakeManagedWindow
            {
                Left = 100_000,
                Top = 100_000,
                Width = 680,
                Height = 420
            };
        var secondWindow =
            new FakeManagedWindow();
        var windows =
            new Queue<IManagedWindow>(
                [firstWindow, secondWindow]
            );
        var manager =
            new WindowManager(
                () => windows.Dequeue()
            );

        manager.ToggleTranslateWindow();
        manager.ToggleTranslateWindow();
        manager.ToggleTranslateWindow();

        Assert.Equal(0, secondWindow.Left);
        Assert.Equal(0, secondWindow.Top);
        Assert.Equal(520, secondWindow.Width);
        Assert.Equal(316, secondWindow.Height);
        Assert.Equal(
            WindowStartupLocation.CenterScreen,
            secondWindow.WindowStartupLocation
        );
    }

    private sealed class FakeManagedWindow
        : IManagedWindow
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; } = 520;

        public double Height { get; set; } = 316;

        public WindowStartupLocation WindowStartupLocation { get; set; } =
            WindowStartupLocation.CenterScreen;

        public WindowState WindowState { get; set; } =
            WindowState.Normal;

        public int ShowCount { get; private set; }

        public int CloseCount { get; private set; }

        public int ActivateCount { get; private set; }

        public event EventHandler? Closed;

        public void Show()
        {
            ShowCount++;
        }

        public bool Activate()
        {
            ActivateCount++;

            return true;
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
