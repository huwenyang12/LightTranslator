using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.ViewModels;

namespace LightTranslator.Tests;

public sealed class OptionalHotkeyDefaultsTests
{
    [Fact]
    public void CreateDefault_LeavesTextAndScreenshotHotkeysUnconfigured()
    {
        var settings =
            AppSettings.CreateDefault();

        Assert.Null(
            settings.TextTranslationHotkey
        );

        Assert.Null(
            settings.ScreenshotTranslationHotkey
        );
    }

    [Fact]
    public void FirstRunSettingsViewModel_StartsWithNoTextTranslationHotkey()
    {
        var viewModel =
            new FirstRunSettingsViewModel();

        Assert.Null(
            viewModel.TextTranslationHotkey
        );
    }

    [Fact]
    public void RegisterTextTranslation_WhenHotkeyIsUnconfigured_SkipsBackendAndSucceeds()
    {
        var backend =
            new RecordingHotkeyBackend();

        var service =
            new HotkeyService(
                backend
            );

        var registered =
            service.RegisterTextTranslation(
                null!
            );

        Assert.True(
            registered
        );

        Assert.Empty(
            backend.RegisterCalls
        );
    }

    [Fact]
    public void RegisterScreenshotTranslation_WhenHotkeyIsUnconfigured_SkipsBackendAndSucceeds()
    {
        var backend =
            new RecordingHotkeyBackend();

        var service =
            new HotkeyService(
                backend
            );

        var registered =
            service.RegisterScreenshotTranslation(
                null!
            );

        Assert.True(
            registered
        );

        Assert.Empty(
            backend.RegisterCalls
        );
    }

    private sealed class RecordingHotkeyBackend
        : IHotkeyBackend
    {
        public event Action<int>? HotkeyPressed
        {
            add
            {
            }

            remove
            {
            }
        }

        public List<(int Id, uint Modifiers, uint VirtualKey)>
            RegisterCalls
        {
            get;
        } =
            new();

        public bool Register(
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            RegisterCalls.Add(
                (
                    id,
                    modifiers,
                    virtualKey
                )
            );

            return true;
        }

        public void Unregister(
            int id
        )
        {
        }
    }
}
