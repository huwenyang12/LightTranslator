using System.Reflection;
using System.Windows;
using LightTranslator.Services.Tray;

namespace LightTranslator.Tests;

public sealed class TrayMenuPlacementTests
{
    [Theory]
    [InlineData(990, 790, 810, 690)]
    [InlineData(0, 0, 8, 8)]
    public void Calculate_KeepsMenuInsideWorkingArea(
        double cursorX,
        double cursorY,
        double expectedX,
        double expectedY
    )
    {
        var type =
            typeof(NotifyIconTrayBackend).Assembly.GetType(
                "LightTranslator.Services.Tray.TrayMenuPlacement"
            );

        Assert.NotNull(type);

        var calculate =
            type.GetMethod(
                "Calculate",
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

        Assert.NotNull(calculate);

        var result =
            Assert.IsType<Point>(
                calculate.Invoke(
                    null,
                    [
                        cursorX,
                        cursorY,
                        180d,
                        100d,
                        new Rect(0, 0, 1000, 800),
                        8d
                    ]
                )
            );

        Assert.Equal(expectedX, result.X);
        Assert.Equal(expectedY, result.Y);
    }
}
