using System.Windows;
using System.Windows.Media.Imaging;
using LightTranslator.Models;

namespace LightTranslator.Services.ScreenCapture;

public sealed class DisplayCaptureService
    : IDisplayCaptureService
{
    private readonly IDisplayCaptureBackend _backend;

    public DisplayCaptureService()
        : this(
            new WindowsDisplayCaptureBackend()
        )
    {
    }

    public DisplayCaptureService(
        IDisplayCaptureBackend backend
    )
    {
        _backend =
            backend ??
            throw new ArgumentNullException(
                nameof(
                    backend
                )
            );
    }

    public ScreenCaptureFrame CaptureMonitorAtCursor()
    {
        return
            _backend.CaptureMonitorAtCursor();
    }

    public CapturedSelection Crop(
        ScreenCaptureFrame frame,
        PixelRect monitorRelativeBounds
    )
    {
        ArgumentNullException.ThrowIfNull(
            frame
        );

        ValidateSelection(
            frame,
            monitorRelativeBounds
        );

        var croppedImage =
            new CroppedBitmap(
                frame.Image,
                new Int32Rect(
                    monitorRelativeBounds.X,
                    monitorRelativeBounds.Y,
                    monitorRelativeBounds.Width,
                    monitorRelativeBounds.Height
                )
            );

        croppedImage.Freeze();

        var screenBounds =
            new PixelRect(
                checked(
                    frame.MonitorBounds.X +
                    monitorRelativeBounds.X
                ),
                checked(
                    frame.MonitorBounds.Y +
                    monitorRelativeBounds.Y
                ),
                monitorRelativeBounds.Width,
                monitorRelativeBounds.Height
            );

        return
            new CapturedSelection(
                croppedImage,
                monitorRelativeBounds,
                screenBounds,
                frame.DpiX,
                frame.DpiY
            );
    }

    private static void ValidateSelection(
        ScreenCaptureFrame frame,
        PixelRect bounds
    )
    {
        if (
            bounds.IsEmpty ||
            bounds.X < 0 ||
            bounds.Y < 0 ||
            bounds.Width > frame.Image.PixelWidth ||
            bounds.Height > frame.Image.PixelHeight ||
            bounds.X > frame.Image.PixelWidth - bounds.Width ||
            bounds.Y > frame.Image.PixelHeight - bounds.Height
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    bounds
                ),
                "Selection must stay inside the captured monitor."
            );
        }
    }
}
