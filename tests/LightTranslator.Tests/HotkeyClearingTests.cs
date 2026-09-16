using LightTranslator.Models;
using LightTranslator.Services.Hotkeys;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public sealed class HotkeyClearingTests
{
    [Fact]
    public async Task SaveTextTranslationHotkeyAsync_WhenCleared_AppliesNullHotkey()
    {
        var oldHotkey = CreateHotkey("T");
        var changeService = new RecordingTextHotkeyChangeService();
        var viewModel = new FirstRunSettingsViewModel(
            hotkeyChangeService: changeService,
            currentTextTranslationHotkey: oldHotkey
        );

        viewModel.TextTranslationHotkey = null;

        var saved = await viewModel.SaveTextTranslationHotkeyAsync();

        Assert.True(saved);
        Assert.Equal(1, changeService.ApplyCallCount);
        Assert.Equal(oldHotkey, changeService.OldHotkey);
        Assert.Null(changeService.NewHotkey);
        Assert.Null(viewModel.TextTranslationHotkey);
    }

    [Fact]
    public async Task SaveScreenshotTranslationHotkeyAsync_WhenCleared_AppliesNullHotkey()
    {
        var oldHotkey = CreateHotkey("Q");
        var changeService = new RecordingScreenshotHotkeyChangeService();
        var viewModel = new FirstRunSettingsViewModel(
            screenshotHotkeyChangeService: changeService,
            currentScreenshotTranslationHotkey: oldHotkey
        );

        viewModel.ScreenshotTranslationHotkey = null;

        var saved = await viewModel.SaveScreenshotTranslationHotkeyAsync();

        Assert.True(saved);
        Assert.Equal(1, changeService.ApplyCallCount);
        Assert.Equal(oldHotkey, changeService.OldHotkey);
        Assert.Null(changeService.NewHotkey);
        Assert.Null(viewModel.ScreenshotTranslationHotkey);
    }

    [Fact]
    public void TextHotkeyBox_Delete_ClearsConfiguredHotkey()
    {
        RunHotkeyClearUiTest(
            isScreenshot: false,
            key: System.Windows.Input.Key.Delete
        );
    }

    [Fact]
    public void ScreenshotHotkeyBox_Backspace_ClearsConfiguredHotkey()
    {
        RunHotkeyClearUiTest(
            isScreenshot: true,
            key: System.Windows.Input.Key.Back
        );
    }

    private static void RunHotkeyClearUiTest(
        bool isScreenshot,
        System.Windows.Input.Key key
    )
    {
        Exception? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    var oldHotkey = CreateHotkey(
                        isScreenshot ? "Q" : "T"
                    );

                    var viewModel = new FirstRunSettingsViewModel(
                        currentTextTranslationHotkey:
                            isScreenshot ? null : oldHotkey,
                        currentScreenshotTranslationHotkey:
                            isScreenshot ? oldHotkey : null
                    );

                    var window = new FirstRunSettingsWindow(
                        viewModel,
                        isFirstRun: false
                    );

                    window.Show();
                    window.Dispatcher.Invoke(
                        () => { },
                        System.Windows.Threading.DispatcherPriority.DataBind
                    );

                    var boxName = isScreenshot
                        ? "ScreenshotTranslationHotkeyBox"
                        : "TextTranslationHotkeyBox";

                    var hotkeyBox =
                        (System.Windows.Controls.TextBox)
                        window.FindName(boxName);

                    hotkeyBox.RaiseEvent(
                        new System.Windows.RoutedEventArgs(
                            System.Windows.UIElement.GotFocusEvent
                        )
                    );

                    var source =
                        System.Windows.PresentationSource.FromVisual(window);

                    var keyEvent =
                        new System.Windows.Input.KeyEventArgs(
                            System.Windows.Input.Keyboard.PrimaryDevice,
                            source!,
                            0,
                            key
                        )
                        {
                            RoutedEvent =
                                System.Windows.Input.Keyboard.PreviewKeyDownEvent
                        };

                    hotkeyBox.RaiseEvent(keyEvent);

                    if (isScreenshot)
                    {
                        Assert.Null(viewModel.ScreenshotTranslationHotkey);
                    }
                    else
                    {
                        Assert.Null(viewModel.TextTranslationHotkey);
                    }

                    Assert.Equal(string.Empty, hotkeyBox.Text);
                    Assert.True(keyEvent.Handled);

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

    private static HotkeyDefinition CreateHotkey(string key)
    {
        return new HotkeyDefinition(
            key,
            Alt: true,
            Control: false,
            Shift: false,
            Windows: false
        );
    }

    private sealed class RecordingTextHotkeyChangeService
        : ITextTranslationHotkeyChangeService
    {
        public int ApplyCallCount { get; private set; }
        public HotkeyDefinition? OldHotkey { get; private set; }
        public HotkeyDefinition? NewHotkey { get; private set; }

        public Task<bool> ApplyAsync(
            HotkeyDefinition oldHotkey,
            HotkeyDefinition newHotkey,
            CancellationToken cancellationToken = default
        )
        {
            ApplyCallCount++;
            OldHotkey = oldHotkey;
            NewHotkey = newHotkey;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingScreenshotHotkeyChangeService
        : IScreenshotTranslationHotkeyChangeService
    {
        public int ApplyCallCount { get; private set; }
        public HotkeyDefinition? OldHotkey { get; private set; }
        public HotkeyDefinition? NewHotkey { get; private set; }

        public Task<bool> ApplyAsync(
            HotkeyDefinition? oldHotkey,
            HotkeyDefinition newHotkey,
            CancellationToken cancellationToken = default
        )
        {
            ApplyCallCount++;
            OldHotkey = oldHotkey;
            NewHotkey = newHotkey;
            return Task.FromResult(true);
        }
    }
}
