using LightTranslator.ViewModels;
using LightTranslator.Services.Settings;
namespace LightTranslator.Tests;

public class FirstRunSettingsViewModelTests
{

    [Fact]
    public async Task SaveStartWithWindowsAsync_PersistsCurrentValue()
    {
        var startupPersistence =
            new FakeStartWithWindowsSettingsPersistence();

        var viewModel =
            new FirstRunSettingsViewModel(
                startupPersistence:
                    startupPersistence
            )
            {
                StartWithWindows = true
            };

        await viewModel.SaveStartWithWindowsAsync();

        Assert.True(
            startupPersistence.SavedValue
        );
    }


    [Fact]
    public void CanSave_WhenApiKeyIsEmpty_ReturnsFalse()
    {
        var viewModel =
            new FirstRunSettingsViewModel();

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public void CanSave_WhenApiKeyHasValue_ReturnsTrue()
    {
        var viewModel =
            new FirstRunSettingsViewModel
            {
                ApiKey = "test-key"
            };

        Assert.True(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task SaveAsync_WhenApiKeyHasValue_PersistsApiKey()
    {
        var persistence =
            new FakeFirstRunSettingsPersistence();

        var viewModel =
            new FirstRunSettingsViewModel(
                persistence
            )
            {
                ApiKey = "test-key"
            };

        var saved =
            await viewModel.SaveAsync();

        Assert.True(
            saved
        );

        Assert.Equal(
            "test-key",
            persistence.SavedApiKey
        );
    }

    private sealed class FakeFirstRunSettingsPersistence
        : IFirstRunSettingsPersistence
    {
        public string? SavedApiKey { get; private set; }

        public Task SaveAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            SavedApiKey =
                apiKey;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStartWithWindowsSettingsPersistence
        : IStartWithWindowsSettingsPersistence
    {
        public bool SavedValue { get; private set; }

        public Task SaveAsync(
            bool enabled,
            CancellationToken cancellationToken = default
        )
        {
            SavedValue =
                enabled;

            return Task.CompletedTask;
        }
    }
}