using System.Xml.Linq;
using System.Text.RegularExpressions;

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
                "Brush.Selection.Selected",
                "Brush.Selection.Hover",
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

    [Fact]
    public void ButtonsAndLanguageSelectors_DoNotAddFocusBorders()
    {
        var stylesDirectory =
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "LightTranslator",
                "Resources",
                "Styles"
            );

        var buttonsDocument =
            XDocument.Load(
                Path.Combine(stylesDirectory, "Buttons.xaml")
            );
        var inputsDocument =
            XDocument.Load(
                Path.Combine(stylesDirectory, "Inputs.xaml")
            );

        Assert.DoesNotContain(
            buttonsDocument.Descendants(PresentationNamespace + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("Property") ==
                    "IsKeyboardFocused"
        );

        var comboBoxStyle =
            inputsDocument
                .Descendants(PresentationNamespace + "Style")
                .Single(
                    style =>
                        (string?)style.Attribute(XamlNamespace + "Key") ==
                            "Style.Input.ComboBox"
                );

        Assert.DoesNotContain(
            comboBoxStyle.Descendants(PresentationNamespace + "Trigger"),
            trigger =>
                (string?)trigger.Attribute("Property") ==
                    "IsKeyboardFocusWithin"
        );
    }

    [Fact]
    public void SharedButtons_SuppressTheDefaultFocusAdorner()
    {
        var document =
            XDocument.Load(
                Path.Combine(
                    FindRepositoryRoot(),
                    "src",
                    "LightTranslator",
                    "Resources",
                    "Styles",
                    "Buttons.xaml"
                )
            );

        foreach (
            var styleKey in new[]
            {
                "Style.Button.Base",
                "Style.Button.IconCircle"
            }
        )
        {
            var style =
                document
                    .Descendants(PresentationNamespace + "Style")
                    .Single(
                        candidate =>
                            (string?)candidate.Attribute(XamlNamespace + "Key") ==
                                styleKey
                    );

            Assert.Contains(
                style.Elements(PresentationNamespace + "Setter"),
                setter =>
                    (string?)setter.Attribute("Property") ==
                        "FocusVisualStyle" &&
                    (string?)setter.Attribute("Value") == "{x:Null}"
            );
        }
    }

    [Fact]
    public void LanguageOptions_UseNeutralSelectionColors()
    {
        var document =
            XDocument.Load(
                Path.Combine(
                    FindRepositoryRoot(),
                    "src",
                    "LightTranslator",
                    "Resources",
                    "Styles",
                    "Inputs.xaml"
                )
            );

        var itemStyle =
            document
                .Descendants(PresentationNamespace + "Style")
                .Single(
                    style =>
                        (string?)style.Attribute(XamlNamespace + "Key") ==
                            "Style.Input.ComboBoxItem"
                );

        AssertTriggerUsesNeutralColors(
            itemStyle,
            "IsSelected",
            "{DynamicResource Brush.Selection.Selected}"
        );
        AssertTriggerUsesNeutralColors(
            itemStyle,
            "IsHighlighted",
            "{DynamicResource Brush.Selection.Hover}"
        );
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
    public void InputStyles_ProvideCenteredTextCleanFocusAndSwitchVisuals()
    {
        var document =
            XDocument.Load(
                Path.Combine(
                    FindRepositoryRoot(),
                    "src",
                    "LightTranslator",
                    "Resources",
                    "Styles",
                    "Inputs.xaml"
                )
            );

        XElement FindStyle(string key) =>
            document
                .Descendants(PresentationNamespace + "Style")
                .Single(
                    element =>
                        (string?)element.Attribute(
                            XamlNamespace + "Key"
                        ) == key
                );

        static bool HasSetter(
            XElement style,
            string property,
            string value
        ) =>
            style
                .Elements(PresentationNamespace + "Setter")
                .Any(
                    setter =>
                        (string?)setter.Attribute("Property") == property &&
                        (string?)setter.Attribute("Value") == value
                );

        var textBoxStyle = FindStyle("Style.Input.TextBox");
        var comboBoxStyle = FindStyle("Style.Input.ComboBox");
        var checkBoxStyle = FindStyle("Style.Input.CheckBox");

        Assert.True(
            HasSetter(
                textBoxStyle,
                "VerticalContentAlignment",
                "Center"
            )
        );
        Assert.True(
            HasSetter(
                textBoxStyle,
                "Padding",
                "12,0"
            )
        );
        Assert.True(
            HasSetter(
                textBoxStyle,
                "FocusVisualStyle",
                "{x:Null}"
            )
        );
        Assert.True(
            HasSetter(
                comboBoxStyle,
                "FocusVisualStyle",
                "{x:Null}"
            )
        );

        var textContentHost =
            document
                .Descendants(PresentationNamespace + "ControlTemplate")
                .Single(
                    template =>
                        (string?)template.Attribute(
                            XamlNamespace + "Key"
                        ) == "Template.Input.TextBox"
                )
                .Descendants(PresentationNamespace + "ScrollViewer")
                .Single(
                    scrollViewer =>
                        (string?)scrollViewer.Attribute(
                            XamlNamespace + "Name"
                        ) == "PART_ContentHost"
                );

        Assert.Equal(
            "False",
            (string?)textContentHost.Attribute("Focusable")
        );
        Assert.Null(
            textContentHost.Attribute(
                "VerticalContentAlignment"
            )
        );

        var templateNames =
            checkBoxStyle
                .Descendants()
                .Select(
                    element =>
                        (string?)element.Attribute(
                            XamlNamespace + "Name"
                        )
                )
                .Where(name => name is not null)
                .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("SwitchTrack", templateNames);
        Assert.Contains("SwitchThumb", templateNames);
        Assert.Contains("SwitchFocusRing", templateNames);

        var keyboardFocusTrigger =
            checkBoxStyle
                .Descendants(PresentationNamespace + "Trigger")
                .Single(
                    trigger =>
                        (string?)trigger.Attribute("Property") ==
                            "IsKeyboardFocused" &&
                        (string?)trigger.Attribute("Value") == "True"
                );

        Assert.Contains(
            keyboardFocusTrigger.Elements(
                PresentationNamespace + "Setter"
            ),
            setter =>
                (string?)setter.Attribute("TargetName") ==
                    "SwitchFocusRing" &&
                (string?)setter.Attribute("Property") ==
                    "BorderBrush" &&
                (string?)setter.Attribute("Value") ==
                    "{DynamicResource Brush.Focus}"
        );
    }

    [Fact]
    public void UserFacingWindows_UseOnlySemanticColors()
    {
        var allowedColors =
            new HashSet<string>(
                new[]
                {
                    "#66000000"
                },
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var path in GetUserFacingXamlPaths())
        {
            var text =
                File.ReadAllText(
                    path
                );

            var unexpected =
                Regex.Matches(
                        text,
                        "#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?"
                    )
                    .Select(
                        match =>
                            match.Value
                    )
                    .Where(
                        color =>
                            !allowedColors.Contains(
                                color
                            )
                    )
                    .ToArray();

            Assert.True(
                unexpected.Length == 0,
                $"{Path.GetFileName(path)} contains hard-coded colors: {string.Join(", ", unexpected)}"
            );
        }
    }

    [Fact]
    public void NamedInteractiveControls_HaveAutomationNames()
    {
        var missing =
            new List<string>();

        foreach (var path in GetUserFacingXamlPaths())
        {
            var document =
                XDocument.Load(
                    path
                );

            foreach (
                var element in document
                    .Descendants()
                    .Where(
                        element =>
                            element.Name.LocalName is
                                "Button" or
                                "TextBox" or
                                "PasswordBox" or
                                "ComboBox" or
                                "CheckBox"
                    )
            )
            {
                var name =
                    (string?)element.Attribute(
                        XamlNamespace +
                        "Name"
                    );

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var automationName =
                    element.Attributes()
                        .FirstOrDefault(
                            attribute =>
                                attribute.Name.LocalName ==
                                "AutomationProperties.Name"
                        )
                        ?.Value;

                if (string.IsNullOrWhiteSpace(automationName))
                {
                    missing.Add(
                        $"{Path.GetFileName(path)}:{name}"
                    );
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            $"Missing AutomationProperties.Name: {string.Join(", ", missing)}"
        );
    }

    [Fact]
    public void UserFacingWindows_DoNotDisableTabNavigation()
    {
        foreach (var path in GetUserFacingXamlPaths())
        {
            var text =
                File.ReadAllText(
                    path
                );

            Assert.DoesNotContain(
                "KeyboardNavigation.TabNavigation=\"None\"",
                text,
                StringComparison.Ordinal
            );
        }
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

    private static void AssertTriggerUsesNeutralColors(
        XElement style,
        string triggerProperty,
        string expectedBackground
    )
    {
        var trigger =
            style
                .Descendants(PresentationNamespace + "Trigger")
                .Single(
                    candidate =>
                        (string?)candidate.Attribute("Property") ==
                            triggerProperty &&
                        (string?)candidate.Attribute("Value") == "True"
                );
        var setters =
            trigger
                .Elements(PresentationNamespace + "Setter")
                .ToArray();

        Assert.Contains(
            setters,
            setter =>
                (string?)setter.Attribute("Property") == "Background" &&
                (string?)setter.Attribute("Value") == expectedBackground
        );
        Assert.Contains(
            setters,
            setter =>
                (string?)setter.Attribute("Property") == "Foreground" &&
                (string?)setter.Attribute("Value") ==
                    "{DynamicResource Brush.Text.Primary}"
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

    private static IEnumerable<string> GetUserFacingXamlPaths()
    {
        var viewsDirectory =
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "LightTranslator",
                "Views"
            );
        var trayDirectory =
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "LightTranslator",
                "Services",
                "Tray"
            );

        return new[]
        {
            Path.Combine(
                viewsDirectory,
                "TranslateWindow.xaml"
            ),
            Path.Combine(
                viewsDirectory,
                "FirstRunSettingsWindow.xaml"
            ),
            Path.Combine(
                viewsDirectory,
                "ScreenshotCaptureWindow.xaml"
            ),
            Path.Combine(
                viewsDirectory,
                "ScreenshotTranslationWindow.xaml"
            ),
            Path.Combine(
                trayDirectory,
                "TrayMenuWindow.xaml"
            )
        };
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
