using LightTranslator.Services.Tray;

namespace LightTranslator.Tests;

public class TrayServiceTests
{

    [Fact]
    public void BackendRequestsTextTranslation_RaisesTextTranslationRequested()
    {
        var backend =
            new FakeTrayIconBackend();

        var service =
            new TrayService(
                backend
            );

        var raisedCount =
            0;

        service.TextTranslationRequested +=
            () => raisedCount++;

        backend.RaiseTextTranslationRequested();

        Assert.Equal(
            1,
            raisedCount
        );
    }

    [Fact]
    public void BackendRequestsScreenshotTranslation_RaisesScreenshotTranslationRequested()
    {
        var backend =
            new FakeTrayIconBackend();

        var service =
            new TrayService(
                backend
            );

        var raisedCount =
            0;

        service.ScreenshotTranslationRequested +=
            () => raisedCount++;

        backend.RaiseScreenshotTranslationRequested();

        Assert.Equal(
            1,
            raisedCount
        );
    }

    [Fact]
    public void BackendRequestsSettings_RaisesSettingsRequested()
    {
        var backend =
            new FakeTrayIconBackend();

        var service =
            new TrayService(
                backend
            );

        var raisedCount = 0;

        service.SettingsRequested +=
            () => raisedCount++;

        backend.RaiseSettingsRequested();

        Assert.Equal(
            1,
            raisedCount
        );
    }

    [Fact]
    public void BackendRequestsExit_RaisesExitRequested()
    {
        var backend =
            new FakeTrayIconBackend();

        var service =
            new TrayService(
                backend
            );

        var raisedCount = 0;

        service.ExitRequested +=
            () => raisedCount++;

        backend.RaiseExitRequested();

        Assert.Equal(
            1,
            raisedCount
        );
    }

    [Fact]
    public void Start_ShowsTrayIcon()
    {
        var backend =
            new FakeTrayIconBackend();

        var service =
            new TrayService(
                backend
            );

        service.Start();

        Assert.Equal(
            1,
            backend.ShowCount
        );
    }

    private sealed class FakeTrayIconBackend
        : ITrayIconBackend
    {
        public int ShowCount { get; private set; }

        public event Action? SettingsRequested;

        public event Action? ExitRequested;
        public event Action? TextTranslationRequested;
        public event Action? ScreenshotTranslationRequested;

        public void RaiseScreenshotTranslationRequested()
        {
            ScreenshotTranslationRequested?.Invoke();
        }

        public void RaiseTextTranslationRequested()
        {
            TextTranslationRequested?.Invoke();
        }

        public void Show()
        {
            ShowCount++;
        }

        public void Hide()
        {
        }

        public void Dispose()
        {
        }

        public void RaiseSettingsRequested()
        {
            SettingsRequested?.Invoke();
        }

        public void RaiseExitRequested()
        {
            ExitRequested?.Invoke();
        }
    }
}