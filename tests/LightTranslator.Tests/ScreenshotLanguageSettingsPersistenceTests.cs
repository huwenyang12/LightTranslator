using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public sealed class ScreenshotLanguageSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAsync_UpdatesOnlyScreenshotLanguages()
    {
        var original =
            AppSettings.CreateDefault() with
            {
                TextSourceLanguage =
                    "en",

                TextTargetLanguage =
                    "zh",

                ScreenshotSourceLanguage =
                    "auto",

                ScreenshotTargetLanguage =
                    "zh"
            };

        var settingsService =
            new FakeSettingsService(
                original
            );

        var persistence =
            new ScreenshotLanguageSettingsPersistence(
                settingsService
            );

        await persistence.SaveAsync(
            "ja",
            "en"
        );

        var saved =
            Assert.IsType<AppSettings>(
                settingsService.SavedSettings
            );

        Assert.Equal(
            "ja",
            saved.ScreenshotSourceLanguage
        );

        Assert.Equal(
            "en",
            saved.ScreenshotTargetLanguage
        );

        Assert.Equal(
            "en",
            saved.TextSourceLanguage
        );

        Assert.Equal(
            "zh",
            saved.TextTargetLanguage
        );
    }

    [Theory]
    [InlineData(
        "fr",
        "zh"
    )]
    [InlineData(
        "auto",
        "fr"
    )]
    [InlineData(
        "zh",
        "zh"
    )]
    [InlineData(
        "en",
        "en"
    )]
    [InlineData(
        "ja",
        "ja"
    )]
    public async Task SaveAsync_RejectsUnsupportedOrIdenticalExplicitLanguages(
        string sourceLanguage,
        string targetLanguage
    )
    {
        var settingsService =
            new FakeSettingsService(
                AppSettings.CreateDefault()
            );

        var persistence =
            new ScreenshotLanguageSettingsPersistence(
                settingsService
            );

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                persistence.SaveAsync(
                    sourceLanguage,
                    targetLanguage
                )
        );

        Assert.Null(
            settingsService.SavedSettings
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
            return
                Task.FromResult(
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

            return
                Task.CompletedTask;
        }
    }
}
