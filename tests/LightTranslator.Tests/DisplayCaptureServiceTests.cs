using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.ScreenCapture;

namespace LightTranslator.Tests;

public class DisplayCaptureServiceTests
{
    [Fact]
    public void CaptureMonitorAtCursor_ReturnsFrozenBackendFrame()
    {
        var expected =
            CreateFrame(
                width: 800,
                height: 600,
                screenX: -800,
                screenY: 100,
                dpi: 120
            );

        var backend =
            new FakeDisplayCaptureBackend(
                expected
            );

        var service =
            new DisplayCaptureService(
                backend
            );

        var actual =
            service.CaptureMonitorAtCursor();

        Assert.Same(expected, actual);
        Assert.True(actual.Image.IsFrozen);
        Assert.Equal(1, backend.CaptureCount);
    }

    [Fact]
    public void Crop_PreservesAbsoluteScreenBoundsAndDpi()
    {
        var frame =
            CreateFrame(
                width: 800,
                height: 600,
                screenX: -800,
                screenY: 100,
                dpi: 120
            );

        var service =
            new DisplayCaptureService(
                new FakeDisplayCaptureBackend(
                    frame
                )
            );

        var result =
            service.Crop(
                frame,
                new PixelRect(
                    100,
                    50,
                    200,
                    80
                )
            );

        Assert.Equal(
            new PixelRect(
                100,
                50,
                200,
                80
            ),
            result.MonitorRelativeBounds
        );

        Assert.Equal(
            new PixelRect(
                -700,
                150,
                200,
                80
            ),
            result.ScreenBounds
        );

        Assert.Equal(120, result.DpiX);
        Assert.Equal(120, result.DpiY);
        Assert.Equal(200, result.Image.PixelWidth);
        Assert.Equal(80, result.Image.PixelHeight);
        Assert.True(result.Image.IsFrozen);
    }

    [Theory]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(0, -1, 10, 10)]
    [InlineData(790, 10, 20, 20)]
    [InlineData(10, 590, 20, 20)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 10, 0)]
    public void Crop_RejectsSelectionOutsideCurrentMonitor(
        int x,
        int y,
        int width,
        int height
    )
    {
        var frame =
            CreateFrame(
                width: 800,
                height: 600,
                screenX: 0,
                screenY: 0,
                dpi: 96
            );

        var service =
            new DisplayCaptureService(
                new FakeDisplayCaptureBackend(
                    frame
                )
            );

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                service.Crop(
                    frame,
                    new PixelRect(
                        x,
                        y,
                        width,
                        height
                    )
                )
        );
    }

    [Theory]
    [InlineData(0, 96)]
    [InlineData(96, 0)]
    [InlineData(-1, 96)]
    [InlineData(96, -1)]
    public void ScreenCaptureFrame_WithInvalidDpi_Throws(
        double dpiX,
        double dpiY
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ScreenCaptureFrame(
                    CreateBitmap(
                        10,
                        10,
                        96
                    ),
                    new PixelRect(
                        0,
                        0,
                        10,
                        10
                    ),
                    dpiX,
                    dpiY
                )
        );
    }

    private static ScreenCaptureFrame CreateFrame(
        int width,
        int height,
        int screenX,
        int screenY,
        double dpi
    )
    {
        return
            new ScreenCaptureFrame(
                CreateBitmap(
                    width,
                    height,
                    dpi
                ),
                new PixelRect(
                    screenX,
                    screenY,
                    width,
                    height
                ),
                dpi,
                dpi
            );
    }

    private static BitmapSource CreateBitmap(
        int width,
        int height,
        double dpi
    )
    {
        var bitmap =
            new WriteableBitmap(
                width,
                height,
                dpi,
                dpi,
                PixelFormats.Bgra32,
                null
            );

        bitmap.Freeze();

        return bitmap;
    }

    private sealed class FakeDisplayCaptureBackend
        : IDisplayCaptureBackend
    {
        private readonly ScreenCaptureFrame _frame;

        public FakeDisplayCaptureBackend(
            ScreenCaptureFrame frame
        )
        {
            _frame =
                frame;
        }

        public int CaptureCount
        {
            get;
            private set;
        }

        public ScreenCaptureFrame CaptureMonitorAtCursor()
        {
            CaptureCount++;

            return _frame;
        }
    }
}
