using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public interface IScreenshotTranslationHotkeyPersistence
{
    Task<bool> SaveAsync(
        HotkeyDefinition hotkey,
        CancellationToken cancellationToken = default
    );
}
