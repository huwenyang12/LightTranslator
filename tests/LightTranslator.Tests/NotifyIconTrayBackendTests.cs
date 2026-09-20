using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using LightTranslator.Services.Tray;

namespace LightTranslator.Tests;

public sealed class NotifyIconTrayBackendTests
{
    [Fact]
    public void NotifyIconTrayBackend_ImplementsTrayIconBackendContract()
    {
        Assert.True(
            typeof(ITrayIconBackend).IsAssignableFrom(
                typeof(NotifyIconTrayBackend)
            )
        );
    }

    [Fact]
    public void NotifyIcon_DoesNotAttachLegacyContextMenu()
    {
        using var backend = new NotifyIconTrayBackend();
        var notifyIcon = GetNotifyIcon(backend);

        Assert.Null(notifyIcon.ContextMenuStrip);
    }

    [Fact]
    public void TrayMenu_ContainsRequiredCommandsInOrder()
    {
        RunOnSta(
            () =>
            {
                var window = CreateTrayMenuWindow();
                var commandPanel =
                    Assert.IsType<StackPanel>(
                        window.FindName("CommandPanel")
                    );

                var commandTexts =
                    commandPanel.Children
                        .OfType<Button>()
                        .Select(button => button.Content)
                        .Cast<string>()
                        .ToArray();

                Assert.Equal(
                    ["文本翻译", "截图翻译", "设置", "退出"],
                    commandTexts
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void TrayMenu_UsesCompactRoundedSurface()
    {
        RunOnSta(
            () =>
            {
                var window = CreateTrayMenuWindow();
                var surface =
                    Assert.IsType<Border>(
                        window.FindName("MenuSurface")
                    );

                Assert.Equal(WindowStyle.None, window.WindowStyle);
                Assert.True(window.AllowsTransparency);
                Assert.False(window.ShowInTaskbar);
                Assert.True(window.Topmost);
                Assert.Equal(new CornerRadius(10), surface.CornerRadius);
                Assert.Equal(new Thickness(1), surface.BorderThickness);

                var commandPanel =
                    Assert.IsType<StackPanel>(
                        window.FindName("CommandPanel")
                    );

                Assert.Equal(
                    KeyboardNavigationMode.Cycle,
                    KeyboardNavigation.GetDirectionalNavigation(
                        commandPanel
                    )
                );
                Assert.Equal(
                    KeyboardNavigationMode.Cycle,
                    KeyboardNavigation.GetTabNavigation(
                        commandPanel
                    )
                );

                window.Close();
            }
        );
    }

    [Theory]
    [InlineData("TextTranslationButton", "TextTranslationRequested")]
    [InlineData("ScreenshotTranslationButton", "ScreenshotTranslationRequested")]
    [InlineData("SettingsButton", "SettingsRequested")]
    [InlineData("ExitButton", "ExitRequested")]
    public void TrayMenu_CommandClick_RaisesCorrespondingEvent(
        string buttonName,
        string eventName
    )
    {
        RunOnSta(
            () =>
            {
                var window = CreateTrayMenuWindow();
                var eventInfo =
                    Assert.IsAssignableFrom<EventInfo>(
                        window.GetType().GetEvent(eventName)
                    );
                var invocationCount = 0;
                Action handler = () => invocationCount++;

                eventInfo.AddEventHandler(window, handler);

                var button =
                    Assert.IsType<Button>(
                        window.FindName(buttonName)
                    );

                button.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent)
                );

                Assert.Equal(1, invocationCount);

                window.Close();
            }
        );
    }

    [Fact]
    public void ShowTrayMenu_ShowsSingleMenuAndForwardsCommand()
    {
        RunOnSta(
            () =>
            {
                var menu = new FakeTrayMenu();
                var factoryCalls = 0;

                using var backend =
                    new NotifyIconTrayBackend(
                        () =>
                        {
                            factoryCalls++;
                            return menu;
                        },
                        Dispatcher.CurrentDispatcher
                    );

                var textTranslationCount = 0;

                backend.TextTranslationRequested +=
                    () => textTranslationCount++;

                backend.ShowTrayMenu();
                backend.ShowTrayMenu();

                Assert.Equal(1, factoryCalls);
                Assert.Equal(1, menu.ShowCount);

                menu.RequestTextTranslation();

                Assert.Equal(1, menu.CloseCount);
                Assert.Equal(1, textTranslationCount);
            }
        );
    }

    private static Window CreateTrayMenuWindow()
    {
        var type =
            typeof(NotifyIconTrayBackend).Assembly.GetType(
                "LightTranslator.Services.Tray.TrayMenuWindow"
            );

        Assert.NotNull(type);

        return Assert.IsAssignableFrom<Window>(
            Activator.CreateInstance(type)
        );
    }

    private static Forms.NotifyIcon GetNotifyIcon(
        NotifyIconTrayBackend backend
    )
    {
        var field =
            typeof(NotifyIconTrayBackend).GetField(
                "_notifyIcon",
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        Assert.NotNull(field);

        return Assert.IsType<Forms.NotifyIcon>(
            field.GetValue(backend)
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

    private sealed class FakeTrayMenu : ITrayMenu
    {
        public event Action? TextTranslationRequested;
        public event Action? ScreenshotTranslationRequested;
        public event Action? SettingsRequested;
        public event Action? ExitRequested;
        public event EventHandler? Closed;

        public bool IsVisible { get; private set; }
        public int ShowCount { get; private set; }
        public int CloseCount { get; private set; }

        public void ShowAtCursor()
        {
            IsVisible = true;
            ShowCount++;
        }

        public void Close()
        {
            if (!IsVisible)
            {
                return;
            }

            IsVisible = false;
            CloseCount++;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        public void RequestTextTranslation()
        {
            TextTranslationRequested?.Invoke();
        }

        public void RequestScreenshotTranslation()
        {
            ScreenshotTranslationRequested?.Invoke();
        }

        public void RequestSettings()
        {
            SettingsRequested?.Invoke();
        }

        public void RequestExit()
        {
            ExitRequested?.Invoke();
        }
    }
}
