using System.IO;

namespace LightTranslator.Services.Logging;

public static class LogFilePathProvider
{
    public static string GetDefaultPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "LightTranslator",
            "logs",
            "app.log"
        );
    }
}