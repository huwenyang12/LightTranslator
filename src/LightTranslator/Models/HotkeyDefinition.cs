namespace LightTranslator.Models;

public sealed record HotkeyDefinition(
    string Key,
    bool Alt,
    bool Control,
    bool Shift,
    bool Windows
);