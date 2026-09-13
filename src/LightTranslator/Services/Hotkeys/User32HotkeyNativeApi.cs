using System.Runtime.InteropServices;

namespace LightTranslator.Services.Hotkeys;

public sealed class User32HotkeyNativeApi
    : IHotkeyNativeApi
{
    public bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey
    )
    {
        return RegisterHotKeyNative(
            windowHandle,
            id,
            modifiers,
            virtualKey
        );
    }

    public bool UnregisterHotKey(
        IntPtr windowHandle,
        int id
    )
    {
        return UnregisterHotKeyNative(
            windowHandle,
            id
        );
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "RegisterHotKey",
        SetLastError = true
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKeyNative(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk
    );

    [DllImport(
        "user32.dll",
        EntryPoint = "UnregisterHotKey",
        SetLastError = true
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKeyNative(
        IntPtr hWnd,
        int id
    );
}