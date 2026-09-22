using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class ApplicationVersionDisplayTests
{
    [Fact]
    public void Format_PrefersInformationalVersionAndRemovesMetadata()
    {
        Assert.Equal(
            "1.2.3",
            ApplicationVersionDisplay.Format(
                "1.2.3+build.45",
                new Version(9, 8, 7, 6)
            )
        );
    }

    [Fact]
    public void Format_WhenInformationalVersionIsBlank_UsesAssemblyVersion()
    {
        Assert.Equal(
            "0.3.0",
            ApplicationVersionDisplay.Format(
                "  ",
                new Version(0, 3, 0, 0)
            )
        );
    }

    [Fact]
    public void Format_WhenVersionsAreUnavailable_UsesSafeFallback()
    {
        Assert.Equal(
            "0.0.0",
            ApplicationVersionDisplay.Format(
                null,
                null
            )
        );
    }
}
