namespace LightTranslator.Services.Hotkeys;

public interface IHotkeyNativeApi
{
    bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey
    );

    bool UnregisterHotKey(
        IntPtr windowHandle,
        int id
    );
}