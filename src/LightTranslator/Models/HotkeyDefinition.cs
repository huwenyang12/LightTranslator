namespace LightTranslator.Models;

public sealed record HotkeyDefinition(
    string Key,
    bool Alt,
    bool Control,
    bool Shift,
    bool Windows
)
{
    public static bool TryCreate(
        string key,
        bool alt,
        bool control,
        bool shift,
        bool windows,
        out HotkeyDefinition? hotkey
    )
    {
        if (
            string.IsNullOrWhiteSpace(key) ||
            key.Length != 1 ||
            (
                !alt &&
                !control &&
                !shift &&
                !windows
            )
        )
        {
            hotkey =
                null;

            return false;
        }

        hotkey =
            new HotkeyDefinition(
                key.ToUpperInvariant(),
                alt,
                control,
                shift,
                windows
            );

        return true;
    }
}