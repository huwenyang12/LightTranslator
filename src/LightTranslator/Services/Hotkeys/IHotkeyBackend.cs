namespace LightTranslator.Services.Hotkeys;

public interface IHotkeyBackend
{
    event Action<int>? HotkeyPressed;

    bool Register(
        int id,
        uint modifiers,
        uint virtualKey
    );

    void Unregister(
        int id
    );
}