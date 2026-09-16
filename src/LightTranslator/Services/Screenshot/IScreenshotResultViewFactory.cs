using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public interface IScreenshotResultViewFactory
{
    IScreenshotResultView Create(
        CapturedSelection selection
    );
}
