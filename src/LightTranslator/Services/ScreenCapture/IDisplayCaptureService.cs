using LightTranslator.Models;

namespace LightTranslator.Services.ScreenCapture;

public interface IDisplayCaptureService
{
    ScreenCaptureFrame CaptureMonitorAtCursor();

    CapturedSelection Crop(
        ScreenCaptureFrame frame,
        PixelRect monitorRelativeBounds
    );
}
