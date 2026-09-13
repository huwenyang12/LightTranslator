using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var dir = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var service = new SettingsService(dir);

        var settings = await service.LoadAsync();

        Assert.Equal(
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false),
            settings.TextTranslationHotkey);

        Assert.Equal("auto", settings.TextSourceLanguage);
        Assert.Equal("zh", settings.TextTargetLanguage);
        Assert.False(settings.FirstRunCompleted);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsSettings()
    {
        var dir = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var service = new SettingsService(dir);

        var expected = AppSettings.CreateDefault() with
        {
            FirstRunCompleted = true,
            TextTargetLanguage = "en"
        };

        await service.SaveAsync(expected);

        var actual = await service.LoadAsync();

        Assert.Equal(expected, actual);
    }
}