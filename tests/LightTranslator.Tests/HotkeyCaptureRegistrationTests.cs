using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public sealed class HotkeyCaptureRegistrationTests
{
    [Fact]
    public void BeginTextTranslationCapture_ReleasesConfiguredTextHotkey()
    {
        var backend = new FakeHotkeyBackend();
        var service = new HotkeyService(backend);

        service.RegisterTextTranslation(
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            )
        );

        backend.UnregisterCalls.Clear();

        service.BeginTextTranslationCapture();

        Assert.Equal(
            new[] { HotkeyService.TextTranslationHotkeyId },
            backend.UnregisterCalls
        );
    }

    [Fact]
    public void EndTextTranslationCapture_RestoresConfiguredTextHotkey()
    {
        var backend = new FakeHotkeyBackend();
        var service = new HotkeyService(backend);

        var hotkey = new HotkeyDefinition(
            "T",
            Alt: true,
            Control: false,
            Shift: false,
            Windows: false
        );

        service.RegisterTextTranslation(hotkey);
        backend.RegisterCalls.Clear();

        service.BeginTextTranslationCapture();
        service.EndTextTranslationCapture();

        var call = Assert.Single(backend.RegisterCalls);

        Assert.Equal(
            HotkeyService.TextTranslationHotkeyId,
            call.Id
        );
        Assert.Equal(0x0001u, call.Modifiers);
        Assert.Equal((uint)'T', call.VirtualKey);
    }

    [Fact]
    public void BeginScreenshotTranslationCapture_ReleasesConfiguredScreenshotHotkey()
    {
        var backend = new FakeHotkeyBackend();
        var service = new HotkeyService(backend);

        service.RegisterScreenshotTranslation(
            new HotkeyDefinition(
                "Q",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            )
        );

        backend.UnregisterCalls.Clear();

        service.BeginScreenshotTranslationCapture();

        Assert.Equal(
            new[] { HotkeyService.ScreenshotTranslationHotkeyId },
            backend.UnregisterCalls
        );
    }

    [Fact]
    public void EndScreenshotTranslationCapture_RestoresConfiguredScreenshotHotkey()
    {
        var backend = new FakeHotkeyBackend();
        var service = new HotkeyService(backend);

        var hotkey = new HotkeyDefinition(
            "Q",
            Alt: true,
            Control: false,
            Shift: false,
            Windows: false
        );

        service.RegisterScreenshotTranslation(hotkey);
        backend.RegisterCalls.Clear();

        service.BeginScreenshotTranslationCapture();
        service.EndScreenshotTranslationCapture();

        var call = Assert.Single(backend.RegisterCalls);

        Assert.Equal(
            HotkeyService.ScreenshotTranslationHotkeyId,
            call.Id
        );
        Assert.Equal(0x0001u, call.Modifiers);
        Assert.Equal((uint)'Q', call.VirtualKey);
    }

    private sealed class FakeHotkeyBackend : IHotkeyBackend
    {
        public event Action<int>? HotkeyPressed;

        public List<(int Id, uint Modifiers, uint VirtualKey)>
            RegisterCalls { get; } = new();

        public List<int> UnregisterCalls { get; } = new();

        public bool Register(
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            RegisterCalls.Add((id, modifiers, virtualKey));
            return true;
        }

        public void Unregister(int id)
        {
            UnregisterCalls.Add(id);
        }

        public void RaiseHotkey(int id)
        {
            HotkeyPressed?.Invoke(id);
        }
    }
}
