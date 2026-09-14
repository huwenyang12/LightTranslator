using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class TextTranslationHotkeyChangeServiceTests
{

    [Fact]
    public async Task ApplyAsync_WhenPersistenceFails_RestoresOldHotkey()
    {
        var backend =
            new FakeHotkeyBackend(
                new[]
                {
                    true,
                    true
                }
            );

        var hotkeyService =
            new HotkeyService(
                backend
            );

        var persistence =
            new FakeTextTranslationHotkeyPersistence
            {
                SaveResult =
                    false
            };

        var service =
            new TextTranslationHotkeyChangeService(
                hotkeyService,
                persistence
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

        var changed =
            await service.ApplyAsync(
                oldHotkey,
                newHotkey
            );

        Assert.False(
            changed
        );

        Assert.Equal(
            2,
            backend.RegisterCalls.Count
        );

        Assert.Equal(
            (uint)'Q',
            backend.RegisterCalls[0].VirtualKey
        );

        Assert.Equal(
            (uint)'T',
            backend.RegisterCalls[1].VirtualKey
        );
    }
    [Fact]
    public async Task ApplyAsync_WhenNewHotkeyRegistrationFails_DoesNotPersist()
    {
        var backend =
            new FakeHotkeyBackend(
                new[]
                {
                    false,
                    true
                }
            );

        var hotkeyService =
            new HotkeyService(
                backend
            );

        var persistence =
            new FakeTextTranslationHotkeyPersistence();

        var service =
            new TextTranslationHotkeyChangeService(
                hotkeyService,
                persistence
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

        var changed =
            await service.ApplyAsync(
                oldHotkey,
                newHotkey
            );

        Assert.False(
            changed
        );

        Assert.Equal(
            0,
            persistence.SaveCallCount
        );
    }


    private sealed class FakeTextTranslationHotkeyPersistence
        : ITextTranslationHotkeyPersistence
    {
        public bool SaveResult
        {
            get;
            set;
        } =
            true;
        public int SaveCallCount
        {
            get;
            private set;
        }

        public Task<bool> SaveAsync(
            HotkeyDefinition hotkey,
            CancellationToken cancellationToken = default
        )
        {
            SaveCallCount++;

            return Task.FromResult(
                SaveResult
            );
        }
    }


    private sealed class FakeHotkeyBackend
        : IHotkeyBackend
    {
        private readonly Queue<bool> _registerResults;

        public FakeHotkeyBackend(
            IEnumerable<bool> registerResults
        )
        {
            _registerResults =
                new Queue<bool>(
                    registerResults
                );
        }

        public List<(uint Modifiers, uint VirtualKey)>
            RegisterCalls
        {
            get;
        } =
            new();

        public event Action<int>? HotkeyPressed
        {
            add
            {
            }

            remove
            {
            }
        }

        public bool Register(
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            RegisterCalls.Add(
                (
                    modifiers,
                    virtualKey
                )
            );

            if (_registerResults.Count > 0)
            {
                return _registerResults.Dequeue();
            }

            return true;
        }

        public void Unregister(
            int id
        )
        {
        }
    }
}