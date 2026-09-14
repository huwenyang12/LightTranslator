using LightTranslator.Models;

namespace LightTranslator.Controllers;

public sealed class AppController
{
    private readonly IAppStartupView _startupView;

    private readonly IScreenshotTranslationView?
        _screenshotTranslationView;

    public AppController(
        IAppStartupView startupView,
        IScreenshotTranslationView? screenshotTranslationView = null
    )
    {
        _startupView =
            startupView;

        _screenshotTranslationView =
            screenshotTranslationView;
    }

    public bool Start(
        AppSettings settings
    )
    {
        if (
            settings.FirstRunCompleted
        )
        {
            return true;
        }

        return _startupView.ShowFirstRunSettings();
    }

    public void OpenSettings()
    {
        _startupView.ShowSettings();
    }

    public void OpenScreenshotTranslation()
    {
        _screenshotTranslationView?
            .ShowScreenshotTranslation();
    }
}