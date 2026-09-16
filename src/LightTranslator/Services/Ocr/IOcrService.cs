using System.Windows.Media.Imaging;
using LightTranslator.Models;

namespace LightTranslator.Services.Ocr;

public interface IOcrService
{
    Task<IReadOnlyList<OcrBlock>> RecognizeAsync(
        BitmapSource image,
        CancellationToken cancellationToken = default
    );
}
