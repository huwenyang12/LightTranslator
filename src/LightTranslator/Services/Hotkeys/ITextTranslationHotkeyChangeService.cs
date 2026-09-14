using LightTranslator.Models;

namespace LightTranslator.Services.Hotkeys;

public interface ITextTranslationHotkeyChangeService
{
    Task<bool> ApplyAsync(
        HotkeyDefinition oldHotkey,
        HotkeyDefinition newHotkey,
        CancellationToken cancellationToken = default
    );
}