using System.Windows;
using Microsoft.Win32;

using WpfApplication =
    System.Windows.Application;

namespace LightTranslator.Services.Windows;

public sealed class ThemeManager
    : IDisposable
{
    private const string PersonalizeRegistryPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightThemeValueName =
        "AppsUseLightTheme";

    private const string PaletteSourceMarker =
        "/Resources/Theme/Palette.";

    private WpfApplication? _application;
    private bool _isStarted;

    public void Start(
        WpfApplication application
    )
    {
        ArgumentNullException.ThrowIfNull(
            application
        );

        if (_isStarted)
        {
            return;
        }

        _application =
            application;

        Apply(
            application,
            ReadCurrentAppearance()
        );

        SystemEvents.UserPreferenceChanged +=
            OnUserPreferenceChanged;

        _isStarted =
            true;
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -=
            OnUserPreferenceChanged;

        _application =
            null;

        _isStarted =
            false;
    }

    public void Dispose()
    {
        Stop();
    }

    public static void Apply(
        WpfApplication application,
        SystemAppearance appearance
    )
    {
        ArgumentNullException.ThrowIfNull(
            application
        );

        var dictionaries =
            application.Resources.MergedDictionaries;

        var currentPalette =
            dictionaries.FirstOrDefault(
                dictionary =>
                    dictionary.Source?.OriginalString.Contains(
                        PaletteSourceMarker,
                        StringComparison.Ordinal
                    ) == true
            );

        var replacement =
            new ResourceDictionary
            {
                Source =
                    GetPaletteSource(
                        appearance
                    )
            };

        if (currentPalette is null)
        {
            dictionaries.Insert(
                0,
                replacement
            );

            return;
        }

        var index =
            dictionaries.IndexOf(
                currentPalette
            );

        dictionaries[index] =
            replacement;
    }

    internal static Uri GetPaletteSource(
        SystemAppearance appearance
    )
    {
        var fileName =
            appearance switch
            {
                SystemAppearance.Dark =>
                    "Palette.Dark.xaml",
                SystemAppearance.HighContrast =>
                    "Palette.HighContrast.xaml",
                _ =>
                    "Palette.Light.xaml"
            };

        return new Uri(
            $"/Bridgo;component/Resources/Theme/{fileName}",
            UriKind.Relative
        );
    }

    private static SystemAppearance ReadCurrentAppearance()
    {
        var appsUseLightTheme =
            1;

        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(
                    PersonalizeRegistryPath
                );

            if (
                key?.GetValue(
                    AppsUseLightThemeValueName
                ) is int value
            )
            {
                appsUseLightTheme =
                    value;
            }
        }
        catch (
            Exception exception
        ) when (
            exception is System.Security.SecurityException or
            UnauthorizedAccessException
        )
        {
            appsUseLightTheme =
                1;
        }

        return SystemAppearanceResolver.Resolve(
            SystemParameters.HighContrast,
            appsUseLightTheme
        );
    }

    private void OnUserPreferenceChanged(
        object sender,
        UserPreferenceChangedEventArgs e
    )
    {
        var application =
            _application;

        if (application is null)
        {
            return;
        }

        application.Dispatcher.BeginInvoke(
            () =>
                Apply(
                    application,
                    ReadCurrentAppearance()
                )
        );
    }
}
