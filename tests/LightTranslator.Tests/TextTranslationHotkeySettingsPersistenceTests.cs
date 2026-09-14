using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class TextTranslationHotkeySettingsPersistenceTests
{
    [Fact]
    public async Task SaveAsync_UpdatesTextTranslationHotkey()
    {
        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault()
            );

        var persistence =
            new TextTranslationHotkeySettingsPersistence(
                settingsService
            );

        var hotkey =
            new HotkeyDefinition(
                "Q",
                Alt: false,
                Control: true,
                Shift: true,
                Windows: false
            );

        var saved =
            await persistence.SaveAsync(
                hotkey
            );

        Assert.True(
            saved
        );

        Assert.NotNull(
            settingsService.SavedSettings
        );

        Assert.Equal(
            hotkey,
            settingsService.SavedSettings.TextTranslationHotkey
        );
    }


    private sealed class FakeSettingsService
        : ISettingsService
    {
        private readonly AppSettings _settings;

        public FakeSettingsService(
            AppSettings settings
        )
        {
            _settings =
                settings;
        }

        public AppSettings? SavedSettings
        {
            get;
            private set;
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