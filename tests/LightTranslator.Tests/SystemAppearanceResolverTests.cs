using LightTranslator.Services.Windows;

namespace LightTranslator.Tests;

public sealed class SystemAppearanceResolverTests
{
    [Theory]
    [InlineData(true, 1, SystemAppearance.HighContrast)]
    [InlineData(true, 0, SystemAppearance.HighContrast)]
    [InlineData(false, 1, SystemAppearance.Light)]
    [InlineData(false, 0, SystemAppearance.Dark)]
    public void Resolve_ReturnsExpectedAppearance(
        bool highContrast,
        int appsUseLightTheme,
        SystemAppearance expected
    )
    {
        Assert.Equal(
            expected,
            SystemAppearanceResolver.Resolve(
                highContrast,
                appsUseLightTheme
            )
        );
    }

    [Theory]
    [InlineData(SystemAppearance.Light, "/Bridgo;component/Resources/Theme/Palette.Light.xaml")]
    [InlineData(SystemAppearance.Dark, "/Bridgo;component/Resources/Theme/Palette.Dark.xaml")]
    [InlineData(SystemAppearance.HighContrast, "/Bridgo;component/Resources/Theme/Palette.HighContrast.xaml")]
    public void GetPaletteSource_ReturnsAppearanceDictionary(
        SystemAppearance appearance,
        string expected
    )
    {
        Assert.Equal(
            expected,
            ThemeManager.GetPaletteSource(
                appearance
            ).OriginalString
        );
    }
}
