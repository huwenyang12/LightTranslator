using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shell;
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
                    540d,
                    window.Width
                );
                Assert.Equal(
                    300d,
                    window.Height
                );
                Assert.Equal(440d, window.MinWidth);
                Assert.Equal(260d, window.MinHeight);
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
                Assert.IsType<TextBlock>(
                    window.FindName(
                        "KeyboardHintTextBlock"
                    )
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
        var viewModel =
            new TranslateViewModel(
                new FixedTranslationService(),
                TimeSpan.Zero
            );

        viewModel.SourceText = "你好";

        Assert.True(
            SpinWait.SpinUntil(
                () => viewModel.TranslatedText == "Hello",
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
