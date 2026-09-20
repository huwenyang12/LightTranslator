using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
                    280d,
                    window.Height
                );
                Assert.Equal(420d, window.MinWidth);
                Assert.Equal(240d, window.MinHeight);
                Assert.Equal(ResizeMode.CanResize, window.ResizeMode);

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

                Assert.IsType<Button>(
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
    public void EmptyEditors_ShowSourceAndResultPlaceholders()
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

                var sourcePlaceholder =
                    Assert.IsType<TextBlock>(
                        window.FindName("SourcePlaceholderTextBlock")
                    );
                var resultPlaceholder =
                    Assert.IsType<TextBlock>(
                        window.FindName("ResultPlaceholderTextBlock")
                    );

                Assert.Equal(Visibility.Visible, sourcePlaceholder.Visibility);
                Assert.Equal(Visibility.Visible, resultPlaceholder.Visibility);

                window.Close();
            }
        );
    }

    [Fact]
    public void SourceEditor_WhenFocused_KeepsSinglePixelFocusBorder()
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

                window.Show();
                window.Activate();
                window.SourceTextBox.Focus();
                window.Dispatcher.Invoke(
                    () => { },
                    DispatcherPriority.Input
                );
                window.SourceTextBox.ApplyTemplate();

                var inputBorder =
                    Assert.IsType<Border>(
                        window.SourceTextBox.Template.FindName(
                            "InputBorder",
                            window.SourceTextBox
                        )
                    );

                Assert.Equal(new Thickness(1), inputBorder.BorderThickness);

                window.Close();
            }
        );
    }

    [Fact]
    public void CopyButton_WhenResultIsEmpty_IsDisabled()
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

                var copyButton =
                    Assert.IsType<Button>(
                        window.FindName("CopyTranslationButton")
                    );

                Assert.False(copyButton.IsEnabled);

                window.Close();
            }
        );
    }

    [Fact]
    public void CopyButton_WhenCopySucceeds_ShowsConfirmationState()
    {
        RunOnSta(
            () =>
            {
                var window =
                    new TranslateWindow(
                        CreateTranslatedViewModel(),
                        languagePersistence: null,
                        new FakeClipboardService(true),
                        (_, _) => { }
                    );
                var copyButton =
                    Assert.IsType<Button>(
                        window.FindName("CopyTranslationButton")
                    );

                copyButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent)
                );

                Assert.Equal("✓ 已复制", copyButton.Content);
                Assert.Equal(
                    "已复制翻译结果",
                    AutomationProperties.GetName(copyButton)
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void WhitespaceResult_ShowsPlaceholderAndDisablesCopy()
    {
        RunOnSta(
            () =>
            {
                var viewModel =
                    CreateTranslatedViewModel(
                        new WhitespaceTranslationService(),
                        "   "
                    );
                var window = new TranslateWindow(viewModel);
                var copyButton =
                    Assert.IsType<Button>(
                        window.FindName("CopyTranslationButton")
                    );
                var resultPlaceholder =
                    Assert.IsType<TextBlock>(
                        window.FindName("ResultPlaceholderTextBlock")
                    );

                Assert.False(copyButton.IsEnabled);
                Assert.Equal(Visibility.Visible, resultPlaceholder.Visibility);

                window.Close();
            }
        );
    }

    [Fact]
    public void CopyFeedback_WhenTranslationChanges_ResetsImmediately()
    {
        RunOnSta(
            () =>
            {
                var viewModel =
                    CreateTranslatedViewModel(
                        new EchoTranslationService(),
                        "你好"
                    );
                var window =
                    new TranslateWindow(
                        viewModel,
                        languagePersistence: null,
                        new FakeClipboardService(true),
                        (_, _) => { }
                    );
                var copyButton =
                    Assert.IsType<Button>(
                        window.FindName("CopyTranslationButton")
                    );

                copyButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent)
                );
                Assert.Equal("✓ 已复制", copyButton.Content);

                viewModel.SourceText = "第二条";

                Assert.Equal("第二条", viewModel.TranslatedText);
                Assert.Equal("复制", copyButton.Content);
                Assert.Equal(
                    "复制翻译结果",
                    AutomationProperties.GetName(copyButton)
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void CopyFeedback_UsesBriefOneHundredFortyMillisecondAnimation()
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
                var storyboard =
                    Assert.IsType<Storyboard>(
                        window.FindResource("Storyboard.CopyFeedback.In")
                    );
                var animation =
                    Assert.IsType<DoubleAnimation>(
                        Assert.Single(storyboard.Children)
                    );

                Assert.Equal(
                    TimeSpan.FromMilliseconds(140),
                    animation.Duration.TimeSpan
                );

                window.Close();
            }
        );
    }

    [Fact]
    public void CopyButton_WhenClipboardStaysBusy_ShowsErrorWithoutThrowing()
    {
        RunOnSta(
            () =>
            {
                var viewModel = CreateTranslatedViewModel();
                var clipboard = new FakeClipboardService(false);
                string? errorMessage = null;
                Window? errorOwner = null;
                var window =
                    new TranslateWindow(
                        viewModel,
                        languagePersistence: null,
                        clipboard,
                        (owner, message) =>
                        {
                            errorOwner = owner;
                            errorMessage = message;
                        }
                    );

                var copyButton =
                    Assert.IsType<Button>(
                        window.FindName("CopyTranslationButton")
                    );

                var exception =
                    Record.Exception(
                        () => copyButton.RaiseEvent(
                            new RoutedEventArgs(Button.ClickEvent)
                        )
                    );

                Assert.Null(exception);
                Assert.Same(window, errorOwner);
                Assert.Equal("复制失败，请重试。", errorMessage);
                Assert.Equal("Hello", clipboard.LastText);

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

    private sealed class WhitespaceTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult("   ", null)
            );
        }
    }

    private sealed class EchoTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult(request.Text, null)
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
