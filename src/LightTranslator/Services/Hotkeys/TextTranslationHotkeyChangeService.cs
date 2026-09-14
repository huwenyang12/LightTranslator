using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Services.Hotkeys;

public sealed class TextTranslationHotkeyChangeService
    : ITextTranslationHotkeyChangeService
{
    private readonly HotkeyService _hotkeyService;

    private readonly ITextTranslationHotkeyPersistence
        _persistence;


    public TextTranslationHotkeyChangeService(
        HotkeyService hotkeyService,
        ITextTranslationHotkeyPersistence persistence
    )
    {
        _hotkeyService =
            hotkeyService;

        _persistence =
            persistence;
    }


    public async Task<bool> ApplyAsync(
        HotkeyDefinition oldHotkey,
        HotkeyDefinition newHotkey,
        CancellationToken cancellationToken = default
    )
    {
        var replaced =
            _hotkeyService.ReplaceTextTranslation(
                oldHotkey,
                newHotkey
            );

        if (!replaced)
        {
            return false;
        }

        var persisted =
            await _persistence.SaveAsync(
                newHotkey,
                cancellationToken
            );

        if (persisted)
        {
            return true;
        }

        _hotkeyService.ReplaceTextTranslation(
            newHotkey,
            oldHotkey
        );

        return false;
    }
}