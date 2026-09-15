using System.Windows.Media.Imaging;

namespace LightTranslator.Models;

public sealed class CapturedSelection
{
    public CapturedSelection(
        BitmapSource image,
        PixelRect monitorRelativeBounds,
        PixelRect screenBounds,
        double dpiX,
        double dpiY
    )
    {
        ArgumentNullException.ThrowIfNull(
            image
        );

        if (
            monitorRelativeBounds.IsEmpty ||
            screenBounds.IsEmpty
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    monitorRelativeBounds
                )
            );
        }

        if (
            image.PixelWidth != monitorRelativeBounds.Width ||
            image.PixelHeight != monitorRelativeBounds.Height ||
            screenBounds.Width != monitorRelativeBounds.Width ||
            screenBounds.Height != monitorRelativeBounds.Height
        )
        {
            throw new ArgumentException(
                "Image and selection dimensions must match."
            );
        }

        if (
            dpiX <= 0 ||
            dpiY <= 0 ||
            !double.IsFinite(
                dpiX
            ) ||
            !double.IsFinite(
                dpiY
            )
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    dpiX
                )
            );
        }

        if (!image.IsFrozen)
        {
            image.Freeze();
        }

        Image =
            image;

        MonitorRelativeBounds =
            monitorRelativeBounds;

        ScreenBounds =
            screenBounds;

        DpiX =
            dpiX;

        DpiY =
            dpiY;
    }

    public BitmapSource Image
    {
        get;
    }

    public PixelRect MonitorRelativeBounds
    {
        get;
    }

    public PixelRect ScreenBounds
    {
        get;
    }

    public double DpiX
    {
        get;
    }

    public double DpiY
    {
        get;
    }
}
