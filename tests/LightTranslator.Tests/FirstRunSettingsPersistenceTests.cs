using LightTranslator.Infrastructure.Security;
using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class FirstRunSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAsync_SavesApiKeyAndMarksFirstRunCompleted()
    {
        var secretStorage =
            new FakeSecretStorage();

        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault()
            );

        var persistence =
            new FirstRunSettingsPersistence(
                secretStorage,
                settingsService
            );

        await persistence.SaveAsync(
            "test-key"
        );

        Assert.Equal(
            "deepseek-api-key",
            secretStorage.SavedName
        );

        Assert.Equal(
            "test-key",
            secretStorage.SavedSecret
        );

        Assert.NotNull(
            settingsService.SavedSettings
        );

        Assert.True(
            settingsService
                .SavedSettings!
                .FirstRunCompleted
        );
    }

    private sealed class FakeSecretStorage
        : ISecretStorage
    {
        public string? SavedName { get; private set; }

        public string? SavedSecret { get; private set; }

        public void Save(
            string name,
            string secret
        )
        {
            SavedName =
                name;

            SavedSecret =
                secret;
        }

        public string? Load(
            string name
        )
        {
            return null;
        }

        public void Delete(
            string name
        )
        {
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