using LightTranslator.Models;

namespace LightTranslator.Controllers;

public sealed class AppController
{
    private readonly IAppStartupView _startupView;

    public AppController(
        IAppStartupView startupView
    )
    {
        _startupView =
            startupView;
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
}