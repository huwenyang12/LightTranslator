namespace LightTranslator.Services.Hotkeys;

public sealed class HotkeyMessageRouter
{
    public const int WmHotkey = 0x0312;

    public event Action<int>? HotkeyPressed;

    public void ProcessMessage(
        int message,
        IntPtr wParam
    )
    {
        if (message != WmHotkey)
        {
            return;
        }

        var hotkeyId =
            wParam.ToInt32();

        HotkeyPressed?.Invoke(
            hotkeyId
        );
    }
}