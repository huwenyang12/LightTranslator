using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using LightTranslator.Models;

using DrawingBitmap =
    System.Drawing.Bitmap;

using DrawingGraphics =
    System.Drawing.Graphics;

using DrawingPixelFormat =
    System.Drawing.Imaging.PixelFormat;

using NativePoint =
    System.Drawing.Point;

namespace LightTranslator.Services.ScreenCapture;

public sealed class WindowsDisplayCaptureBackend
    : IDisplayCaptureBackend
{
    private const uint MonitorDefaultToNearest =
        2;

    private const int EffectiveDpi =
        0;

    public ScreenCaptureFrame CaptureMonitorAtCursor()
    {
        var cursorPosition =
            Cursor.Position;

        var screen =
            Screen.FromPoint(
                cursorPosition
            );

        var bounds =
            screen.Bounds;

        var monitorBounds =
            new PixelRect(
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height
            );

        var (
            dpiX,
            dpiY
        ) =
            GetMonitorDpi(
                cursorPosition
            );

        var image =
            Capture(
                bounds
            );

        return
            new ScreenCaptureFrame(
                image,
                monitorBounds,
                dpiX,
                dpiY
            );
    }

    private static BitmapSource Capture(
        Rectangle bounds
    )
    {
        using var bitmap =
            new DrawingBitmap(
                bounds.Width,
                bounds.Height,
                DrawingPixelFormat.Format32bppArgb
            );

        using (
            var graphics =
                DrawingGraphics.FromImage(
                    bitmap
                )
        )
        {
            graphics.CopyFromScreen(
                bounds.Left,
                bounds.Top,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy
            );
        }

        var handle =
            bitmap.GetHbitmap();

        try
        {
            var source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );

            source.Freeze();

            return source;
        }
        finally
        {
            DeleteObject(
                handle
            );
        }
    }

    private static (
        double DpiX,
        double DpiY
    ) GetMonitorDpi(
        NativePoint cursorPosition
    )
    {
        try
        {
            var monitor =
                MonitorFromPoint(
                    cursorPosition,
                    MonitorDefaultToNearest
                );

            if (
                monitor != IntPtr.Zero &&
                GetDpiForMonitor(
                    monitor,
                    EffectiveDpi,
                    out var dpiX,
                    out var dpiY
                ) == 0
            )
            {
                return (
                    dpiX,
                    dpiY
                );
            }
        }
        catch (
            DllNotFoundException
        )
        {
        }
        catch (
            EntryPointNotFoundException
        )
        {
        }

        return (
            96d,
            96d
        );
    }

    [DllImport(
        "user32.dll"
    )]
    private static extern IntPtr MonitorFromPoint(
        NativePoint point,
        uint flags
    );

    [DllImport(
        "Shcore.dll"
    )]
    private static extern int GetDpiForMonitor(
        IntPtr monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY
    );

    [DllImport(
        "gdi32.dll"
    )]
    [return: MarshalAs(
        UnmanagedType.Bool
    )]
    private static extern bool DeleteObject(
        IntPtr value
    );
}
