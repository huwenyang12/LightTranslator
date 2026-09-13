namespace LightTranslator.Services.Hotkeys;

public sealed class Win32HotkeyBackend
    : IHotkeyBackend
{
    private readonly IntPtr _windowHandle;

    private readonly IHotkeyNativeApi _nativeApi;

    public event Action<int>? HotkeyPressed;

    public Win32HotkeyBackend(
        IntPtr windowHandle,
        IHotkeyNativeApi nativeApi,
        HotkeyMessageRouter router
    )
    {
        _windowHandle =
            windowHandle;

        _nativeApi =
            nativeApi;

        router.HotkeyPressed +=
            OnHotkeyPressed;
    }

    public bool Register(
        int id,
        uint modifiers,
        uint virtualKey
    )
    {
        return _nativeApi.RegisterHotKey(
            _windowHandle,
            id,
            modifiers,
            virtualKey
        );
    }

    public void Unregister(
        int id
    )
    {
        _nativeApi.UnregisterHotKey(
            _windowHandle,
            id
        );
    }

    private void OnHotkeyPressed(
        int id
    )
    {
        HotkeyPressed?.Invoke(
            id
        );
    }
}