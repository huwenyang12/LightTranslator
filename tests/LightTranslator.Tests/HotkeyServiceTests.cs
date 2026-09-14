using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class HotkeyServiceTests
{

    [Fact]
    public void ReplaceTextTranslation_WhenNewHotkeyRegisters_UnregistersOldAndRegistersNew()
    {
        var backend =
            new FakeHotkeyBackend();

        var service =
            new HotkeyService(
                backend
            );

        var oldHotkey =
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            );

        var newHotkey =
            new HotkeyDefinition(
                "Q",
                Alt: false,
                Control: true,
                Shift: true,
                Windows: false
            );

        var replaced =
            service.ReplaceTextTranslation(
                oldHotkey,
                newHotkey
            );

        Assert.True(
            replaced
        );

        Assert.Equal(
            HotkeyService.TextTranslationHotkeyId,
            backend.LastUnregisteredId
        );

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
    public void ReplaceTextTranslation_WhenNewHotkeyFails_RestoresOldHotkey()
    {
        var backend =
            new FakeHotkeyBackend
            {
                RegisterResults =
                    new Queue<bool>(
                        new[]
                        {
                            false,
                            true
                        }
                    )
            };

        var service =
            new HotkeyService(
                backend
            );

        var oldHotkey =
            new HotkeyDefinition(
                "T",
                Alt: true,
                Control: false,
                Shift: false,
                Windows: false
            );

        var newHotkey =
            new HotkeyDefinition(
                "Q",
                Alt: false,
                Control: true,
                Shift: true,
                Windows: false
            );

        var replaced =
            service.ReplaceTextTranslation(
                oldHotkey,
                newHotkey
            );

        Assert.False(
            replaced
        );

        Assert.Equal(
            2,
            backend.RegisterCalls.Count
        );

        Assert.Equal(
            0x0006u,
            backend.RegisterCalls[0].Modifiers
        );

        Assert.Equal(
            (uint)'Q',
            backend.RegisterCalls[0].VirtualKey
        );

        Assert.Equal(
            0x0001u,
            backend.RegisterCalls[1].Modifiers
        );

        Assert.Equal(
            (uint)'T',
            backend.RegisterCalls[1].VirtualKey
        );
    }

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

        public int? LastUnregisteredId
        {
            get;
            private set;
        }

        public Queue<bool> RegisterResults
        {
            get;
            set;
        } =
            new Queue<bool>();

        public List<(uint Modifiers, uint VirtualKey)>
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
            LastId =
                id;

            LastModifiers =
                modifiers;

            LastVirtualKey =
                virtualKey;

            RegisterCalls.Add(
                (
                    modifiers,
                    virtualKey
                )
            );

            if (RegisterResults.Count > 0)
            {
                return RegisterResults.Dequeue();
            }

            return true;
        }

        public void Unregister(
            int id
        )
        {
            LastUnregisteredId =
                id;
        }

        public void RaiseHotkey(
            int id
        )
        {
            HotkeyPressed?.Invoke(id);
        }
    }
}