using LightTranslator.Services.Logging;

namespace LightTranslator.Tests;

public class LogFilePathProviderTests
{
    [Fact]
    public void GetDefaultPath_ReturnsLocalAppDataLogPath()
    {
        var expectedPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "Bridgo",
                "logs",
                "app.log"
            );

        var actualPath =
            LogFilePathProvider.GetDefaultPath();

        Assert.Equal(
            expectedPath,
            actualPath
        );
    }
}
