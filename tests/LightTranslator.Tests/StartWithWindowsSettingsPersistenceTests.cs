using LightTranslator.Models;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Startup;

namespace LightTranslator.Tests;

public class StartWithWindowsSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAsync_WhenEnabled_UpdatesStartupAndSettings()
    {
        var backend =
            new FakeStartupRegistrationBackend();

        var startupService =
            new StartupService(
                backend,
                () => @"C:\Apps\LightTranslator.exe"
            );

        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault()
            );

        var persistence =
            new StartWithWindowsSettingsPersistence(
                startupService,
                settingsService
            );

        await persistence.SaveAsync(
            true
        );

        Assert.Equal(
            @"C:\Apps\LightTranslator.exe",
            backend.RegisteredExecutablePath
        );

        Assert.NotNull(
            settingsService.SavedSettings
        );

        Assert.True(
            settingsService
                .SavedSettings!
                .StartWithWindows
        );
    }

    [Fact]
    public async Task SaveAsync_WhenDisabled_DisablesStartupAndSettings()
    {
        var backend =
            new FakeStartupRegistrationBackend();

        var startupService =
            new StartupService(
                backend,
                () => @"C:\Apps\LightTranslator.exe"
            );

        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault() with
                {
                    StartWithWindows = true
                }
            );

        var persistence =
            new StartWithWindowsSettingsPersistence(
                startupService,
                settingsService
            );

        await persistence.SaveAsync(
            false
        );

        Assert.Equal(
            1,
            backend.DisableCount
        );

        Assert.NotNull(
            settingsService.SavedSettings
        );

        Assert.False(
            settingsService
                .SavedSettings!
                .StartWithWindows
        );
    }

    private sealed class FakeStartupRegistrationBackend
        : IStartupRegistrationBackend
    {
        public string? RegisteredExecutablePath { get; private set; }

        public int DisableCount { get; private set; }

        public void Enable(
            string executablePath
        )
        {
            RegisteredExecutablePath =
                executablePath;
        }

        public void Disable()
        {
            DisableCount++;
        }
    }

    private sealed class FakeSettingsService
        : ISettingsService
    {
        private readonly AppSettings _settings;

        public AppSettings? SavedSettings { get; private set; }

        public FakeSettingsService(
            AppSettings settings
        )
        {
            _settings =
                settings;
        }

        public Task<AppSettings> LoadAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                _settings
            );
        }

        public Task SaveAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default
        )
        {
            SavedSettings =
                settings;

            return Task.CompletedTask;
        }
    }
}