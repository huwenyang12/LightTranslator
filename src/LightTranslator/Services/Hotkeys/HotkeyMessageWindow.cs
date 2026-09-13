using System.Windows.Interop;

namespace LightTranslator.Services.Hotkeys;

public sealed class HotkeyMessageWindow
    : IDisposable
{
    private readonly HotkeyMessageRouter _router;

    private readonly HwndSource _source;

    public IntPtr Handle =>
        _source.Handle;

    public HotkeyMessageWindow(
        HotkeyMessageRouter router
    )
    {
        _router =
            router;

        var parameters =
            new HwndSourceParameters(
                "LightTranslator.HotkeyMessageWindow"
            )
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0
            };

        _source =
            new HwndSource(
                parameters
            );

        _source.AddHook(
            WndProc
        );
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled
    )
    {
        if (
            message ==
            HotkeyMessageRouter.WmHotkey
        )
        {
            _router.ProcessMessage(
                message,
                wParam
            );

            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source.RemoveHook(
            WndProc
        );

        _source.Dispose();
    }
}