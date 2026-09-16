using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.Services.Settings;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationHotkeySettingsTests
{
    [Fact]
    public async Task Persistence_SaveAsync_UpdatesOnlyScreenshotHotkey()
    {
        var textHotkey = new HotkeyDefinition(
            "T",
            Alt: true,
            Control: false,
            Shift: false,
            Windows: false
        );

        var initial = AppSettings.CreateDefault() with
        {
            TextTranslationHotkey = textHotkey,
            ScreenshotSourceLanguage = "en",
            ScreenshotTargetLanguage = "zh"
        };

        var settingsService = new RecordingSettingsService(initial);
        var persistence = new ScreenshotTranslationHotkeySettingsPersistence(
            settingsService
        );

        var screenshotHotkey = new HotkeyDefinition(
            "Q",
            Alt: false,
            Control: true,
            Shift: true,
            Windows: false
        );

        var saved = await persistence.SaveAsync(screenshotHotkey);

        Assert.True(saved);
        Assert.NotNull(settingsService.SavedSettings);
        Assert.Equal(
            screenshotHotkey,
            settingsService.SavedSettings!.ScreenshotTranslationHotkey
        );
        Assert.Equal(
            textHotkey,
            settingsService.SavedSettings.TextTranslationHotkey
        );
        Assert.Equal("en", settingsService.SavedSettings.ScreenshotSourceLanguage);
        Assert.Equal("zh", settingsService.SavedSettings.ScreenshotTargetLanguage);
    }

    [Fact]
    public async Task ChangeService_FromUnconfigured_RegistersAndPersistsScreenshotHotkey()
    {
        var backend = new RecordingHotkeyBackend();
        var hotkeyService = new HotkeyService(backend);
        var persistence = new RecordingScreenshotHotkeyPersistence();
        var changeService = new ScreenshotTranslationHotkeyChangeService(
            hotkeyService,
            persistence
        );

        var hotkey = new HotkeyDefinition(
            "Q",
            Alt: true,
            Control: false,
            Shift: false,
            Windows: false
        );

        var applied = await changeService.ApplyAsync(
            oldHotkey: null,
            newHotkey: hotkey
        );

        Assert.True(applied);
        Assert.Single(backend.RegisterCalls);
        Assert.Equal(
            HotkeyService.ScreenshotTranslationHotkeyId,
            backend.RegisterCalls[0].Id
        );
        Assert.Equal(hotkey, persistence.SavedHotkey);
    }

    [Fact]
    public void ViewModel_ScreenshotHotkeyStartsUnconfiguredAndCanBeCaptured()
    {
        var viewModel = new FirstRunSettingsViewModel();

        Assert.Null(viewModel.ScreenshotTranslationHotkey);

        var hotkey = new HotkeyDefinition(
            "Q",
            Alt: false,
            Control: true,
            Shift: true,
            Windows: false
        );

        viewModel.SetScreenshotTranslationHotkey(hotkey);

        Assert.Equal(hotkey, viewModel.ScreenshotTranslationHotkey);
    }

    [Fact]
    public void SettingsWindow_ContainsScreenshotTranslationHotkeyBox()
    {
        Exception? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    var window = new FirstRunSettingsWindow(
                        new FirstRunSettingsViewModel(),
                        isFirstRun: false
                    );

                    var hotkeyBox = window.FindName(
                        "ScreenshotTranslationHotkeyBox"
                    );

                    Assert.NotNull(hotkeyBox);

                    window.Close();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
        );

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }

    private sealed class RecordingSettingsService : ISettingsService
    {
        private AppSettings _settings;

        public RecordingSettingsService(AppSettings settings)
        {
            _settings = settings;
        }

        public AppSettings? SavedSettings { get; private set; }

        public Task<AppSettings> LoadAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(_settings);
        }

        public Task SaveAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default
        )
        {
            _settings = settings;
            SavedSettings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingScreenshotHotkeyPersistence
        : IScreenshotTranslationHotkeyPersistence
    {
        public HotkeyDefinition? SavedHotkey { get; private set; }

        public Task<bool> SaveAsync(
            HotkeyDefinition hotkey,
            CancellationToken cancellationToken = default
        )
        {
            SavedHotkey = hotkey;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingHotkeyBackend : IHotkeyBackend
    {
        public event Action<int>? HotkeyPressed
        {
            add { }
            remove { }
        }

        public List<(int Id, uint Modifiers, uint VirtualKey)> RegisterCalls
        {
            get;
        } = new();

        public bool Register(int id, uint modifiers, uint virtualKey)
        {
            RegisterCalls.Add((id, modifiers, virtualKey));
            return true;
        }

        public void Unregister(int id)
        {
        }
    }
}
