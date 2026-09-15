using System.Windows.Media.Imaging;

namespace LightTranslator.Models;

public sealed class ScreenCaptureFrame
{
    public ScreenCaptureFrame(
        BitmapSource image,
        PixelRect monitorBounds,
        double dpiX,
        double dpiY
    )
    {
        ArgumentNullException.ThrowIfNull(
            image
        );

        if (monitorBounds.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    monitorBounds
                )
            );
        }

        ValidateDpi(
            dpiX,
            nameof(
                dpiX
            )
        );

        ValidateDpi(
            dpiY,
            nameof(
                dpiY
            )
        );

        if (
            image.PixelWidth != monitorBounds.Width ||
            image.PixelHeight != monitorBounds.Height
        )
        {
            throw new ArgumentException(
                "Image dimensions must match monitor bounds.",
                nameof(
                    image
                )
            );
        }

        if (!image.IsFrozen)
        {
            image.Freeze();
        }

        Image =
            image;

        MonitorBounds =
            monitorBounds;

        DpiX =
            dpiX;

        DpiY =
            dpiY;
    }

    public BitmapSource Image
    {
        get;
    }

    public PixelRect MonitorBounds
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

    private static void ValidateDpi(
        double dpi,
        string parameterName
    )
    {
        if (
            dpi <= 0 ||
            double.IsNaN(
                dpi
            ) ||
            double.IsInfinity(
                dpi
            )
        )
        {
            throw new ArgumentOutOfRangeException(
                parameterName
            );
        }
    }
}
