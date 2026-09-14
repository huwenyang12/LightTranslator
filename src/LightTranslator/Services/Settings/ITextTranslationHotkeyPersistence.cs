using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public interface ITextTranslationHotkeyPersistence
{
    Task<bool> SaveAsync(
        HotkeyDefinition hotkey,
        CancellationToken cancellationToken = default
    );
}