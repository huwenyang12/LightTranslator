using System.Windows;
using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class SettingsWindowCoordinatorTests
{
    [Fact]
    public void RepeatedOpen_ActivatesExistingWindow_ThenAllowsReopenAfterClose()
    {
        RunOnSta(
            () =>
            {
                var coordinator =
                    new SettingsWindowCoordinator();

                Assert.True(coordinator.TryReserveOpen());

                var window =
                    new Window();

                coordinator.Show(window);
                window.WindowState = WindowState.Minimized;

                Assert.False(coordinator.TryReserveOpen());
                Assert.Equal(WindowState.Normal, window.WindowState);
                Assert.True(window.IsVisible);

                window.Close();

                Assert.True(coordinator.TryReserveOpen());
                coordinator.CancelOpen();
            }
        );
    }

    [Fact]
    public void RepeatedOpen_DuringLoading_OnlyReservesOneWindow()
    {
        var coordinator =
            new SettingsWindowCoordinator();

        Assert.True(coordinator.TryReserveOpen());
        Assert.False(coordinator.TryReserveOpen());

        coordinator.CancelOpen();

        Assert.True(coordinator.TryReserveOpen());
        coordinator.CancelOpen();
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

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }
}
