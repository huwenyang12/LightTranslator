using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public interface IScreenshotCaptureView
{
    Task<PixelRect?> SelectAsync(
        ScreenCaptureFrame frame,
        CancellationToken cancellationToken = default
    );
}
