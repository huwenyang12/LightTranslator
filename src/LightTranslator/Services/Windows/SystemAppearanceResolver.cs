namespace LightTranslator.Services.Windows;

public static class SystemAppearanceResolver
{
    public static SystemAppearance Resolve(
        bool highContrast,
        int appsUseLightTheme
    )
    {
        if (highContrast)
        {
            return SystemAppearance.HighContrast;
        }

        return appsUseLightTheme == 0
            ? SystemAppearance.Dark
            : SystemAppearance.Light;
    }
}
