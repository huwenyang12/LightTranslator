using System.Xml.Linq;

namespace LightTranslator.Tests;

public sealed class UiResourceTests
{
    private static readonly XNamespace PresentationNamespace =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace XamlNamespace =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void AppResources_ExposeRequiredSemanticUiKeys()
    {
        var repositoryRoot =
            FindRepositoryRoot();

        var appDocument =
            XDocument.Load(
                Path.Combine(
                    repositoryRoot,
                    "src",
                    "LightTranslator",
                    "App.xaml"
                )
            );

        var mergedSources =
            appDocument
                .Descendants(
                    PresentationNamespace +
                    "ResourceDictionary"
                )
                .Select(
                    element =>
                        (string?)element.Attribute(
                            "Source"
                        )
                )
                .Where(
                    source =>
                        !string.IsNullOrWhiteSpace(
                            source
                        )
                )
                .Select(
                    source =>
                        source!
                )
                .ToArray();

        Assert.Contains(
            "/Bridgo;component/Resources/Theme/Palette.Light.xaml",
            mergedSources
        );
        Assert.Contains(
            "/Bridgo;component/Resources/Theme/Typography.xaml",
            mergedSources
        );
        Assert.Contains(
            "/Bridgo;component/Resources/Theme/Metrics.xaml",
            mergedSources
        );
        Assert.Contains(
            "/Bridgo;component/Resources/Styles/Buttons.xaml",
            mergedSources
        );
        Assert.Contains(
            "/Bridgo;component/Resources/Styles/Inputs.xaml",
            mergedSources
        );
        Assert.Contains(
            "/Bridgo;component/Resources/Styles/Cards.xaml",
            mergedSources
        );

        var requiredKeys =
            new[]
            {
                "Brush.Window.Base",
                "Brush.Surface.Card",
                "Brush.Surface.Elevated",
                "Brush.Text.Primary",
                "Brush.Text.Secondary",
                "Brush.Text.OnAccent",
                "Brush.Border.Subtle",
                "Brush.Accent",
                "Brush.Accent.Hover",
                "Brush.Error",
                "Brush.Success",
                "Brush.Focus",
                "Style.Text.Display",
                "Style.Text.Section",
                "Style.Text.Body",
                "Style.Text.Caption",
                "Space.4",
                "Space.8",
                "Space.12",
                "Space.16",
                "Space.24",
                "Space.32",
                "Radius.8",
                "Radius.10",
                "Radius.14",
                "Radius.18",
                "Height.Control",
                "Height.Button",
                "Style.Button.Primary",
                "Style.Button.Secondary",
                "Style.Button.Quiet",
                "Style.Button.IconCircle",
                "Style.Input.TextBox",
                "Style.Input.PasswordBox",
                "Style.Input.ComboBox",
                "Style.Input.CheckBox",
                "Style.Surface.Card",
                "Style.Surface.Status",
                "Style.Surface.KeyCap",
                "Style.Text.Validation"
            };

        var actualKeys =
            new HashSet<string>(
                mergedSources
                    .SelectMany(
                        source =>
                            LoadResourceKeys(
                                repositoryRoot,
                                source
                            )
                    ),
                StringComparer.Ordinal
            );

        foreach (var key in requiredKeys)
        {
            Assert.Contains(
                key,
                actualKeys
            );
        }
    }

    [Theory]
    [InlineData("Buttons.xaml")]
    [InlineData("Inputs.xaml")]
    [InlineData("Cards.xaml")]
    public void SharedStyleDictionary_UsesSemanticBrushes(
        string fileName
    )
    {
        var path =
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "LightTranslator",
                "Resources",
                "Styles",
                fileName
            );

        var text =
            File.ReadAllText(
                path
            );

        Assert.DoesNotMatch(
            "#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?",
            text
        );
    }

    [Fact]
    public void PaletteDictionaries_ExposeMatchingSemanticKeys()
    {
        var repositoryRoot =
            FindRepositoryRoot();

        var paletteNames =
            new[]
            {
                "Palette.Light.xaml",
                "Palette.Dark.xaml",
                "Palette.HighContrast.xaml"
            };

        var keySets =
            paletteNames
                .Select(
                    name =>
                        LoadKeysFromFile(
                            Path.Combine(
                                repositoryRoot,
                                "src",
                                "LightTranslator",
                                "Resources",
                                "Theme",
                                name
                            )
                        )
                )
                .ToArray();

        Assert.Equal(
            keySets[0].OrderBy(key => key),
            keySets[1].OrderBy(key => key)
        );
        Assert.Equal(
            keySets[0].OrderBy(key => key),
            keySets[2].OrderBy(key => key)
        );
    }

    private static IEnumerable<string> LoadResourceKeys(
        string repositoryRoot,
        string source
    )
    {
        const string componentPrefix =
            "/Bridgo;component/";

        Assert.StartsWith(
            componentPrefix,
            source
        );

        var relativePath =
            source[componentPrefix.Length..]
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar
                );

        return LoadKeysFromFile(
            Path.Combine(
                repositoryRoot,
                "src",
                "LightTranslator",
                relativePath
            )
        );
    }

    private static HashSet<string> LoadKeysFromFile(
        string path
    )
    {
        var document =
            XDocument.Load(
                path
            );

        return document
            .Descendants()
            .Select(
                element =>
                    (string?)element.Attribute(
                        XamlNamespace +
                        "Key"
                    )
            )
            .Where(
                key =>
                    !string.IsNullOrWhiteSpace(
                        key
                    )
            )
            .Select(
                key =>
                    key!
            )
            .ToHashSet(
                StringComparer.Ordinal
            );
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(
                AppContext.BaseDirectory
            );

        while (directory is not null)
        {
            if (
                File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "LightTranslator.sln"
                    )
                )
            )
            {
                return directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate repository root."
        );
    }
}
