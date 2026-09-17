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
        IReadOnlyList<ScreenshotTextRegion> regions
    )
    {
        ArgumentNullException.ThrowIfNull(
            regions
        );

        ShowResults(
            regions
                .Select(
                    region =>
                        new OcrBlock(
                            region.Id,
                            region.Text,
                            region.Confidence,
                            region.Bounds,
                            region.TranslatedText
                        )
                )
                .ToArray()
        );
    }

    void ShowResults(
        IReadOnlyList<OcrBlock> blocks
    )
    {
    }

    void ShowMessage(
        string message
    );

    void Close();
}
