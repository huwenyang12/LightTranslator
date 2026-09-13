using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class HotkeyMessageWindowTests
{
    [Fact]
    public void Create_CreatesNativeWindowHandle()
    {
        Exception? exception = null;

        IntPtr handle =
            IntPtr.Zero;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var router =
                            new HotkeyMessageRouter();

                        using var window =
                            new HotkeyMessageWindow(
                                router
                            );

                        handle =
                            window.Handle;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();

        thread.Join();

        Assert.Null(
            exception
        );

        Assert.NotEqual(
            IntPtr.Zero,
            handle
        );
    }
}