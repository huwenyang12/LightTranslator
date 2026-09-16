using System.Windows;
using System.Windows.Controls;
using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class SettingsHotkeyCaptureLifecycleTests
{
    [Fact]
    public void TextHotkeyBox_FocusAndBlur_ReleasesAndRestoresConfiguredHotkey()
    {
        RunOnSta(
            () =>
            {
                var backend = new FakeHotkeyBackend();
                var hotkeyService = new HotkeyService(backend);
                var hotkey = new HotkeyDefinition(
                    "T",
                    Alt: true,
                    Control: false,
                    Shift: false,
                    Windows: false
                );

                hotkeyService.RegisterTextTranslation(hotkey);
                backend.RegisterCalls.Clear();
                backend.UnregisterCalls.Clear();

                var window = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(
                        currentTextTranslationHotkey: hotkey
                    ),
                    isFirstRun: false,
                    hotkeyService: hotkeyService
                );

                var hotkeyBox = (TextBox)window.FindName(
                    "TextTranslationHotkeyBox"
                );

                hotkeyBox.RaiseEvent(
                    new RoutedEventArgs(UIElement.GotFocusEvent)
                );

                Assert.Equal(
                    new[] { HotkeyService.TextTranslationHotkeyId },
                    backend.UnregisterCalls
                );

                hotkeyBox.RaiseEvent(
                    new RoutedEventArgs(UIElement.LostFocusEvent)
                );

                var restored = Assert.Single(backend.RegisterCalls);

                Assert.Equal(
                    HotkeyService.TextTranslationHotkeyId,
                    restored.Id
                );
                Assert.Equal(0x0001u, restored.Modifiers);
                Assert.Equal((uint)'T', restored.VirtualKey);

                window.Close();
            }
        );
    }

    [Fact]
    public void ScreenshotHotkeyBox_FocusAndBlur_ReleasesAndRestoresConfiguredHotkey()
    {
        RunOnSta(
            () =>
            {
                var backend = new FakeHotkeyBackend();
                var hotkeyService = new HotkeyService(backend);
                var hotkey = new HotkeyDefinition(
                    "Q",
                    Alt: true,
                    Control: false,
                    Shift: false,
                    Windows: false
                );

                hotkeyService.RegisterScreenshotTranslation(hotkey);
                backend.RegisterCalls.Clear();
                backend.UnregisterCalls.Clear();

                var window = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(
                        currentScreenshotTranslationHotkey: hotkey
                    ),
                    isFirstRun: false,
                    hotkeyService: hotkeyService
                );

                var hotkeyBox = (TextBox)window.FindName(
                    "ScreenshotTranslationHotkeyBox"
                );

                hotkeyBox.RaiseEvent(
                    new RoutedEventArgs(UIElement.GotFocusEvent)
                );

                Assert.Equal(
                    new[] { HotkeyService.ScreenshotTranslationHotkeyId },
                    backend.UnregisterCalls
                );

                hotkeyBox.RaiseEvent(
                    new RoutedEventArgs(UIElement.LostFocusEvent)
                );

                var restored = Assert.Single(backend.RegisterCalls);

                Assert.Equal(
                    HotkeyService.ScreenshotTranslationHotkeyId,
                    restored.Id
                );
                Assert.Equal(0x0001u, restored.Modifiers);
                Assert.Equal((uint)'Q', restored.VirtualKey);

                window.Close();
            }
        );
    }

    [Fact]
    public void WindowClose_WhileHotkeyCaptureIsActive_RestoresConfiguredHotkey()
    {
        RunOnSta(
            () =>
            {
                var backend = new FakeHotkeyBackend();
                var hotkeyService = new HotkeyService(backend);
                var hotkey = new HotkeyDefinition(
                    "T",
                    Alt: true,
                    Control: false,
                    Shift: false,
                    Windows: false
                );

                hotkeyService.RegisterTextTranslation(hotkey);
                backend.RegisterCalls.Clear();
                backend.UnregisterCalls.Clear();

                var window = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(
                        currentTextTranslationHotkey: hotkey
                    ),
                    isFirstRun: false,
                    hotkeyService: hotkeyService
                );

                var hotkeyBox = (TextBox)window.FindName(
                    "TextTranslationHotkeyBox"
                );

                hotkeyBox.RaiseEvent(
                    new RoutedEventArgs(UIElement.GotFocusEvent)
                );

                window.Close();

                var restored = Assert.Single(backend.RegisterCalls);

                Assert.Equal(
                    HotkeyService.TextTranslationHotkeyId,
                    restored.Id
                );
                Assert.Equal((uint)'T', restored.VirtualKey);
            }
        );
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    action();
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
