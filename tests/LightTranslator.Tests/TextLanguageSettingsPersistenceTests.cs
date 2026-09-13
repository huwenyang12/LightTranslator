using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class TextLanguageSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAsync_UpdatesTextLanguageSettings()
    {
        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault()
            );

        var persistence =
            new TextLanguageSettingsPersistence(
                settingsService
            );

        await persistence.SaveAsync(
            "ja",
            "en"
        );

        Assert.NotNull(
            settingsService.SavedSettings
        );

        Assert.Equal(
            "ja",
            settingsService
                .SavedSettings!
                .TextSourceLanguage
        );

        Assert.Equal(
            "en",
            settingsService
                .SavedSettings!
                .TextTargetLanguage
        );
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