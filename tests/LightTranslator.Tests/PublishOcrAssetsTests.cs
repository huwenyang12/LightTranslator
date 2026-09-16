using System.Xml.Linq;

namespace LightTranslator.Tests;

public sealed class PublishOcrAssetsTests
{
    [Fact]
    public void OcrAssets_AreExcludedFromSingleFileBundle()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "LightTranslator",
            "LightTranslator.csproj"
        );

        var document = XDocument.Load(projectPath);
        var xmlNamespace = document.Root!.Name.Namespace;

        var ocrContent = document
            .Descendants(xmlNamespace + "Content")
            .Single(
                element =>
                    string.Equals(
                        (string?)element.Attribute("Include"),
                        @"Assets\Ocr\**\*",
                        StringComparison.Ordinal
                    )
            );

        Assert.Equal(
            "true",
            (string?)ocrContent.Element(
                xmlNamespace + "ExcludeFromSingleFile"
            )
        );
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory =
            new DirectoryInfo(AppContext.BaseDirectory);

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

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate repository root."
        );
    }
}
