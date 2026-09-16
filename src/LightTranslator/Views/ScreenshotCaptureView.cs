using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Views;

public sealed class ScreenshotCaptureView
    : IScreenshotCaptureView
{
    public Task<PixelRect?> SelectAsync(
        ScreenCaptureFrame frame,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(
            frame
        );

        cancellationToken.ThrowIfCancellationRequested();

        var window =
            new ScreenshotCaptureWindow(
                frame
            );

        using var cancellationRegistration =
            cancellationToken.Register(
                () =>
                {
                    _ =
                        window.Dispatcher.InvokeAsync(
                            () =>
                            {
                                if (window.IsVisible)
                                {
                                    window.Close();
                                }
                            }
                        );
                }
            );

        var accepted =
            window.ShowDialog() == true;

        cancellationToken.ThrowIfCancellationRequested();

        return
            Task.FromResult(
                accepted
                    ? window.Selection
                    : null
            );
    }
}
