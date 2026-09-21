using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using LightTranslator.Models;
using LightTranslator.Services.Clipboard;
using LightTranslator.Services.Translation;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

[CollectionDefinition(
    "Translate window WPF",
    DisableParallelization = true
)]
public sealed class TranslateWindowWpfCollection
{
}

[Collection("Translate window WPF")]
public sealed class TranslateWindowVisualTests
{
    [Fact]
    public void Window_UsesFloatingSurfaceAndPreservesSourceBinding()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        new TranslateViewModel(
                            new NoOpTranslationService()
                        )
                    );

                Assert.Equal(
                    520d,
                    window.Width
                );
                Assert.Equal(
                    316d,
                    window.Height
                );
                Assert.Equal(420d, window.MinWidth);
                Assert.Equal(276d, window.MinHeight);
                Assert.Equal(ResizeMode.CanResize, window.ResizeMode);
                Assert.True(window.ShowInTaskbar);

                var chrome = WindowChrome.GetWindowChrome(window);

                Assert.NotNull(chrome);
                Assert.Equal(new Thickness(6), chrome.ResizeBorderThickness);
                Assert.Equal(0d, chrome.CaptionHeight);

                var surface =
                    Assert.IsType<Border>(
                        window.FindName(
                            "TranslationSurface"
                        )
                    );

                Assert.Equal(
                    new CornerRadius(
                        0
                    ),
                    surface.CornerRadius
                );

                Assert.Null(
                    window.FindName(
                        "CopyTranslationButton"
                    )
                );
                Assert.IsType<Border>(
                    window.FindName(
                        "ErrorStatusBorder"
                    )
                );
                Assert.Null(
                    window.FindName("KeyboardHintTextBlock")
                );

                var sourceTextBox =
                    Assert.IsType<TextBox>(
                        window.FindName(
                            "SourceTextBox"
                        )
                    );

                Assert.Equal(
                    "SourceText",
                    BindingOperations.GetBinding(
                        sourceTextBox,
                        TextBox.TextProperty
                    )?.Path.Path
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void Header_ShowsBrandAndCloseButtonThatClosesWindow()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        new TranslateViewModel(
                            new NoOpTranslationService()
                        )
                    );

                var header =
                    Assert.IsType<Grid>(
                        window.FindName("TranslationTitleBar")
                    );

                Assert.Equal(36d, header.Height);

                var appIcon =
                    Assert.IsType<Image>(
                        window.FindName("TranslationAppIcon")
                    );
                var iconSource =
                    Assert.IsType<BitmapImage>(appIcon.Source);

                Assert.Equal(20d, appIcon.Width);
                Assert.Equal(20d, appIcon.Height);
                Assert.True(iconSource.PixelWidth > 0);
                Assert.True(iconSource.PixelHeight > 0);

                var closeButton =
                    Assert.IsType<Button>(
                        window.FindName("TranslationCloseButton")
                    );

                Assert.Equal(28d, closeButton.Width);
                Assert.Equal(28d, closeButton.Height);
                Assert.Null(closeButton.ToolTip);

                var sharedTitleBarButtonStyle =
                    window.TryFindResource("Style.Button.TitleBar");

                Assert.NotNull(sharedTitleBarButtonStyle);
                Assert.Same(
                    sharedTitleBarButtonStyle,
                    closeButton.Style
                );

                window.Show();
                closeButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent)
                );

                Assert.False(window.IsVisible);
            }
        );
    }

    [Fact]
    public void EmptyEditors_DoNotShowPlaceholderText()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        new TranslateViewModel(
                            new NoOpTranslationService()
                        )
                    );

                Assert.Null(
                    window.FindName("SourcePlaceholderTextBlock")
                );
                Assert.Null(
                    window.FindName("ResultPlaceholderTextBlock")
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void SourceEditor_WhenFocused_KeepsNeutralSinglePixelBorder()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        new TranslateViewModel(
                            new NoOpTranslationService()
                        )
                    );

                window.Resources["Brush.Border.Subtle"] =
                    Brushes.Gray;
                window.Resources["Brush.Focus"] =
                    Brushes.Blue;

                window.Show();
                window.Activate();
                Assert.True(window.SourceTextBox.Focus());
                window.Dispatcher.Invoke(
                    () => { },
                    DispatcherPriority.Input
                );
                Assert.True(window.SourceTextBox.IsKeyboardFocused);
                window.SourceTextBox.ApplyTemplate();

                var inputBorder =
                    Assert.IsType<Border>(
                        window.SourceTextBox.Template.FindName(
                            "InputBorder",
                            window.SourceTextBox
                        )
                    );

                Assert.Equal(new Thickness(1), inputBorder.BorderThickness);
                Assert.Equal(
                    window.SourceTextBox.BorderBrush,
                    inputBorder.BorderBrush
                );
                Assert.Equal(
                    Colors.Gray,
                    Assert.IsType<SolidColorBrush>(inputBorder.BorderBrush).Color
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void ResultEditor_WhenFocused_RemainsBorderless()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        new TranslateViewModel(
                            new NoOpTranslationService()
                        )
                    );
                var resultTextBox =
                    Assert.IsType<TextBox>(
                        window.FindName("ResultTextBox")
                    );

                window.Show();
                window.Activate();
                Assert.True(resultTextBox.Focus());
                window.Dispatcher.Invoke(
                    () => { },
                    DispatcherPriority.Input
                );
                Assert.True(resultTextBox.IsKeyboardFocused);
                resultTextBox.ApplyTemplate();

                var resultBorder =
                    Assert.IsType<Border>(
                        resultTextBox.Template.FindName(
                            "InputBorder",
                            resultTextBox
                        )
                    );

                Assert.Equal(
                    new Thickness(0),
                    resultBorder.BorderThickness
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void Enter_WhenClipboardStaysBusy_KeepsWindowOpenForRetry()
    {
        RunOnSta(
            () =>
            {
                var viewModel = CreateTranslatedViewModel();
                var clipboard = new FakeClipboardService(false);
                string? errorMessage = null;
                var window =
                    new TranslateWindow(
                        viewModel,
                        languagePersistence: null,
                        clipboard,
                        (_, message) => errorMessage = message
                    );

                window.Show();

                var source = PresentationSource.FromVisual(window);
                var keyEvent =
                    new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source!,
                        0,
                        Key.Enter
                    )
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };

                window.SourceTextBox.RaiseEvent(keyEvent);

                Assert.True(window.IsVisible);
                Assert.True(keyEvent.Handled);
                Assert.Equal("复制失败，请重试。", errorMessage);

                window.Close();
            }
        );
    }

    [Fact]
    public void Enter_WhenResultIsAvailable_CopiesAndClosesWindow()
    {
        RunOnSta(
            () =>
            {
                var clipboard = new FakeClipboardService(true);
                var window =
                    new TranslateWindow(
                        CreateTranslatedViewModel(),
                        languagePersistence: null,
                        clipboard,
                        (_, _) => { }
                    );

                window.Show();

                var source = PresentationSource.FromVisual(window);
                var keyEvent =
                    new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source!,
                        0,
                        Key.Enter
                    )
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };

                window.SourceTextBox.RaiseEvent(keyEvent);

                Assert.False(window.IsVisible);
                Assert.True(keyEvent.Handled);
                Assert.Equal("Hello", clipboard.LastText);
            }
        );
    }

    [Fact]
    public void ImeProcessedEnter_WhenResultIsAvailable_CopiesAndClosesWindow()
    {
        RunOnSta(
            () =>
            {
                var clipboard = new FakeClipboardService(true);
                var window =
                    new TranslateWindow(
                        CreateTranslatedViewModel(),
                        languagePersistence: null,
                        clipboard,
                        (_, _) => { }
                    );

                window.Show();

                var source = PresentationSource.FromVisual(window);
                var keyEvent =
                    new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source!,
                        0,
                        Key.Enter
                    )
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };

                var markImeProcessed =
                    typeof(KeyEventArgs)
                        .GetMethod(
                            "MarkImeProcessed",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic
                        );

                Assert.NotNull(markImeProcessed);
                markImeProcessed.Invoke(keyEvent, null);

                Assert.Equal(Key.ImeProcessed, keyEvent.Key);
                Assert.Equal(Key.Enter, keyEvent.ImeProcessedKey);

                window.SourceTextBox.RaiseEvent(keyEvent);

                Assert.False(window.IsVisible);
                Assert.True(keyEvent.Handled);
                Assert.Equal("Hello", clipboard.LastText);
            }
        );
    }

    private static TranslateViewModel CreateTranslatedViewModel()
    {
        return CreateTranslatedViewModel(
            new FixedTranslationService(),
            "Hello"
        );
    }

    private static TranslateViewModel CreateTranslatedViewModel(
        ITranslationService translationService,
        string expectedTranslation
    )
    {
        var viewModel =
            new TranslateViewModel(
                translationService,
                TimeSpan.Zero
            );

        viewModel.SourceText = "你好";

        Assert.True(
            SpinWait.SpinUntil(
                () => viewModel.TranslatedText == expectedTranslation,
                TimeSpan.FromSeconds(1)
            )
        );

        return viewModel;
    }

    private static void RunOnSta(
        Action action
    )
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        SynchronizationContext.SetSynchronizationContext(
                            new DispatcherSynchronizationContext(
                                Dispatcher.CurrentDispatcher
                            )
                        );

                        action();
                    }
                    catch (
                        Exception caught
                    )
                    {
                        exception =
                            caught;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );
    }

    private sealed class NoOpTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult(
                    string.Empty,
                    null
                )
            );
        }
    }

    private sealed class FixedTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult("Hello", null)
            );
        }
    }

    private sealed class FakeClipboardService(
        bool result
    ) : IClipboardService
    {
        public string? LastText { get; private set; }

        public bool TrySetText(string text)
        {
            LastText = text;
            return result;
        }
    }
}
