using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class HotkeyServiceTests
{

    [Fact]
    public void RegisterTextTranslation_CombinesModifierFlags()
    {
        var backend =
            new FakeHotkeyBackend();

        var service =
            new HotkeyService(backend);

        var hotkey =
            new HotkeyDefinition(
                "Q",
                Alt: false,
                Control: true,
                Shift: true,
                Windows: false
            );

        var registered =
            service.RegisterTextTranslation(
                hotkey
            );

        Assert.True(registered);

        Assert.Equal(
            0x0006u,
            backend.LastModifiers
        );

        Assert.Equal(
            (uint)'Q',
            backend.LastVirtualKey
        );
    }

    [Fact]
    public void BackendRaisesTextTranslationId_RaisesTextTranslationRequested()
    {
        var backend =
            new FakeHotkeyBackend();

        var service =
            new HotkeyService(backend);

        var raisedCount = 0;

        service.TextTranslationRequested +=
            () => raisedCount++;

        backend.RaiseHotkey(
            HotkeyService.TextTranslationHotkeyId
        );

        Assert.Equal(
            1,
            raisedCount
        );
    }

    [Fact]
    public void RegisterTextTranslation_RegistersAltT()
    {
        var backend =
            new FakeHotkeyBackend();

        var service =
            new HotkeyService(backend);

        var hotkey =
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            );

        var registered =
            service.RegisterTextTranslation(
                hotkey
            );

        Assert.True(registered);

        Assert.Equal(
            HotkeyService.TextTranslationHotkeyId,
            backend.LastId
        );

        Assert.Equal(
            0x0001u,
            backend.LastModifiers
        );

        Assert.Equal(
            (uint)'T',
            backend.LastVirtualKey
        );
    }

    private sealed class FakeHotkeyBackend
        : IHotkeyBackend
    {
        public int LastId { get; private set; }

        public uint LastModifiers { get; private set; }

        public uint LastVirtualKey { get; private set; }

        public event Action<int>? HotkeyPressed;

        public bool Register(
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            LastId = id;
            LastModifiers = modifiers;
            LastVirtualKey = virtualKey;

            return true;
        }

        public void Unregister(
            int id
        )
        {
        }

        public void RaiseHotkey(
            int id
        )
        {
            HotkeyPressed?.Invoke(id);
        }
    }
}