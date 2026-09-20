namespace LightTranslator.Services.Clipboard;

internal interface IClipboardService
{
    bool TrySetText(string text);
}
