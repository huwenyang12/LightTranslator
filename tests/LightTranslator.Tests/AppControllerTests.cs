using LightTranslator.Controllers;
using LightTranslator.Models;

namespace LightTranslator.Tests;

public class AppControllerTests
{

    [Fact]
    public void OpenSettings_ShowsNormalSettings()
    {
        var startupView =
            new FakeAppStartupView();

        var controller =
            new AppController(
                startupView
            );

        controller.OpenSettings();

        Assert.Equal(
            1,
            startupView.ShowSettingsCount
        );
    }

    [Fact]
    public void Start_WhenFirstRunCompleted_DoesNotShowFirstRunSettings()
    {
        var startupView =
            new FakeAppStartupView();

        var controller =
            new AppController(
                startupView
            );

        var settings =
            AppSettings.CreateDefault() with
            {
                FirstRunCompleted = true
            };

        controller.Start(
            settings
        );

        Assert.Equal(
            0,
            startupView.ShowFirstRunSettingsCount
        );
    }
    
    [Fact]
    public void Start_WhenFirstRunNotCompleted_ShowsFirstRunSettings()
    {
        var startupView =
            new FakeAppStartupView();

        var controller =
            new AppController(
                startupView
            );

        var settings =
            AppSettings.CreateDefault();

        controller.Start(
            settings
        );

        Assert.Equal(
            1,
            startupView.ShowFirstRunSettingsCount
        );
    }

    private sealed class FakeAppStartupView
        : IAppStartupView
    {
        public int ShowFirstRunSettingsCount { get; private set; }

        public void ShowFirstRunSettings()
        {
            ShowFirstRunSettingsCount++;
        }
        public int ShowSettingsCount { get; private set; }
        public void ShowSettings()
        {
            ShowSettingsCount++;
        }
    }
}