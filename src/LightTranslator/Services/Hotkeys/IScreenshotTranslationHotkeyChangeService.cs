using LightTranslator.Models;

namespace LightTranslator.Services.Hotkeys;

public interface IScreenshotTranslationHotkeyChangeService
{
    Task<bool> ApplyAsync(
        HotkeyDefinition? oldHotkey,
        HotkeyDefinition newHotkey,
        CancellationToken cancellationToken = default
    );
}
