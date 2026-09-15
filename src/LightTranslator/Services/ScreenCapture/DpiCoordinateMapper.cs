using System.Windows;
using LightTranslator.Models;

namespace LightTranslator.Services.ScreenCapture;

public static class DpiCoordinateMapper
{
    private const double DefaultDpi =
        96d;

    public static Rect PixelsToDips(
        PixelRect value,
        double dpiX,
        double dpiY
    )
    {
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

        return
            new Rect(
                value.X *
                DefaultDpi /
                dpiX,
                value.Y *
                DefaultDpi /
                dpiY,
                value.Width *
                DefaultDpi /
                dpiX,
                value.Height *
                DefaultDpi /
                dpiY
            );
    }

    public static PixelRect DipsToPixels(
        Rect value,
        double dpiX,
        double dpiY
    )
    {
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

        return
            new PixelRect(
                (int)Math.Round(
                    value.X *
                    dpiX /
                    DefaultDpi
                ),
                (int)Math.Round(
                    value.Y *
                    dpiY /
                    DefaultDpi
                ),
                (int)Math.Round(
                    value.Width *
                    dpiX /
                    DefaultDpi
                ),
                (int)Math.Round(
                    value.Height *
                    dpiY /
                    DefaultDpi
                )
            );
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
                parameterName,
                dpi,
                "DPI must be a finite positive number."
            );
        }
    }
}
