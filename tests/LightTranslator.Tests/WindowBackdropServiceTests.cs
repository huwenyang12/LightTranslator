using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class WindowBackdropServiceTests
{
    [Theory]
    [InlineData(10, 0, 22621, true)]
    [InlineData(10, 0, 26100, true)]
    [InlineData(10, 0, 22000, false)]
    [InlineData(10, 0, 19045, false)]
    [InlineData(6, 3, 9600, false)]
    public void IsSupported_RequiresWindowsEleven22621(
        int major,
        int minor,
        int build,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            WindowBackdropService.IsSupported(
                new Version(
                    major,
                    minor,
                    build
                )
            )
        );
    }
}
