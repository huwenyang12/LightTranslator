using LightTranslator.Services.Logging;

namespace LightTranslator.Tests;

public class FileLogSinkTests
{

    [Fact]
    public void Write_PrefixesMessageWithTimestamp()
    {
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "LightTranslator.Tests",
                Guid.NewGuid().ToString("N")
            );

        var logFilePath =
            Path.Combine(
                tempDirectory,
                "app.log"
            );

        try
        {
            var sink =
                new FileLogSink(
                    logFilePath
                );

            sink.Write(
                "test message"
            );

            var content =
                File.ReadAllText(
                    logFilePath
                );

            Assert.Matches(
                @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] test message",
                content
            );
        }
        finally
        {
            if (
                Directory.Exists(
                    tempDirectory
                )
            )
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true
                );
            }
        }
    }


    [Fact]
    public void Write_CreatesFileAndAppendsMessage()
    {
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "LightTranslator.Tests",
                Guid.NewGuid().ToString("N")
            );

        var logFilePath =
            Path.Combine(
                tempDirectory,
                "app.log"
            );

        try
        {
            var sink =
                new FileLogSink(
                    logFilePath
                );

            sink.Write(
                "first message"
            );

            sink.Write(
                "second message"
            );

            Assert.True(
                File.Exists(
                    logFilePath
                )
            );

            var content =
                File.ReadAllText(
                    logFilePath
                );

            Assert.Contains(
                "first message",
                content
            );

            Assert.Contains(
                "second message",
                content
            );
        }
        finally
        {
            if (
                Directory.Exists(
                    tempDirectory
                )
            )
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true
                );
            }
        }
    }
}