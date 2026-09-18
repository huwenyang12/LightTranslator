using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LightTranslator.Models;
using LightTranslator.Services.Translation;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

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
                    560d,
                    window.Width
                );
                Assert.Equal(
                    360d,
                    window.Height
                );

                var surface =
                    Assert.IsType<Border>(
                        window.FindName(
                            "TranslationSurface"
                        )
                    );

                Assert.Equal(
                    new CornerRadius(
                        18
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
}
