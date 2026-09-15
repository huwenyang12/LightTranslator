using LightTranslator.Models;

namespace LightTranslator.Services.ScreenCapture;

public interface IDisplayCaptureBackend
{
    ScreenCaptureFrame CaptureMonitorAtCursor();
}
