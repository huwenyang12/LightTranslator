using LightTranslator.Controllers;
using LightTranslator.Models;

namespace LightTranslator.Tests;

public class AppControllerTests
{

    [Fact]
    public void OpenScreenshotTranslation_ShowsScreenshotTranslation()
    {
        var startupView =
            new FakeAppStartupView();

        var screenshotView =
            new FakeScreenshotTranslationView();

        var controller =
            new AppController(
                startupView,
                screenshotView
            );

        controller.OpenScreenshotTranslation();

        Assert.Equal(
            1,
            screenshotView.ShowScreenshotTranslationCount
        );
    }

    [Fact]
    public void Start_WhenFirstRunCancelled_ReturnsFalse()
    {
        var startupView =
            new FakeAppStartupView
            {
                FirstRunAccepted = false
            };

        var controller =
            new AppController(
                startupView
            );

        var settings =
            AppSettings.CreateDefault();

        var shouldContinue =
            controller.Start(
                settings
            );

        Assert.False(
            shouldContinue
        );
    }

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
        public bool FirstRunAccepted { get; set; }

        public int ShowFirstRunSettingsCount { get; private set; }

        public bool ShowFirstRunSettings()
        {
            ShowFirstRunSettingsCount++;

            return FirstRunAccepted;
        }

        public int ShowSettingsCount { get; private set; }

        public void ShowSettings()
        {
            ShowSettingsCount++;
        }
    }

    private sealed class FakeScreenshotTranslationView
        : IScreenshotTranslationView
    {
        public int ShowScreenshotTranslationCount
        {
            get;
            private set;
        }

        public void ShowScreenshotTranslation()
        {
            ShowScreenshotTranslationCount++;
        }
    }
}