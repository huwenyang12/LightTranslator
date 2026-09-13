using LightTranslator.Infrastructure.Security;

namespace LightTranslator.Tests;

public class DpapiSecretStorageTests
{
    [Fact]
    public void SaveLoadDelete_RoundTripsSecretWithoutPlaintextFile()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dir = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N")
        );

        var storage = new DpapiSecretStorage(dir);

        storage.Save(
            "deepseek-api-key",
            "secret-value"
        );

        var loaded = storage.Load("deepseek-api-key");

        Assert.Equal(
            "secret-value",
            loaded
        );

        var bytes = File.ReadAllBytes(
            Path.Combine(
                dir,
                "deepseek-api-key.bin"
            )
        );

        Assert.DoesNotContain(
            "secret-value",
            System.Text.Encoding.UTF8.GetString(bytes)
        );

        storage.Delete("deepseek-api-key");

        Assert.Null(
            storage.Load("deepseek-api-key")
        );
    }
}