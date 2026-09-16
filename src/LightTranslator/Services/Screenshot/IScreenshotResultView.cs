using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public interface IScreenshotResultView
{
    event EventHandler? CloseRequested;

    void ShowLoading(
        CapturedSelection selection,
        string message
    );

    void ShowResults(
        IReadOnlyList<OcrBlock> blocks
    );

    void ShowMessage(
        string message
    );

    void Close();
}
