using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public sealed class HotkeySuspensionTests
{
    [Fact]
    public void SuspendRequests_KeepsHotkeysRegistered_ButSuppressesActions()
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

        service.RegisterScreenshotTranslation(
            new HotkeyDefinition(
                "Q",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            )
        );

        var textRequestedCount = 0;
        var screenshotRequestedCount = 0;

        service.TextTranslationRequested +=
            () => textRequestedCount++;

        service.ScreenshotTranslationRequested +=
            () => screenshotRequestedCount++;

        service.SuspendRequests();

        backend.RaiseHotkey(
            HotkeyService.TextTranslationHotkeyId
        );

        backend.RaiseHotkey(
            HotkeyService.ScreenshotTranslationHotkeyId
        );

        Assert.Equal(0, textRequestedCount);
        Assert.Equal(0, screenshotRequestedCount);
        Assert.Empty(backend.UnregisterCalls);
    }

    [Fact]
    public void ResumeRequests_AfterSuspension_RestoresActions()
    {
        var backend = new FakeHotkeyBackend();
        var service = new HotkeyService(backend);

        var textRequestedCount = 0;
        var screenshotRequestedCount = 0;

        service.TextTranslationRequested +=
            () => textRequestedCount++;

        service.ScreenshotTranslationRequested +=
            () => screenshotRequestedCount++;

        service.SuspendRequests();
        service.ResumeRequests();

        backend.RaiseHotkey(
            HotkeyService.TextTranslationHotkeyId
        );

        backend.RaiseHotkey(
            HotkeyService.ScreenshotTranslationHotkeyId
        );

        Assert.Equal(1, textRequestedCount);
        Assert.Equal(1, screenshotRequestedCount);
    }

    private sealed class FakeHotkeyBackend
        : IHotkeyBackend
    {
        public event Action<int>? HotkeyPressed;

        public List<int> UnregisterCalls
        {
            get;
        } = new();

        public bool Register(
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            return true;
        }

        public void Unregister(
            int id
        )
        {
            UnregisterCalls.Add(id);
        }

        public void RaiseHotkey(
            int id
        )
        {
            HotkeyPressed?.Invoke(id);
        }
    }
}
