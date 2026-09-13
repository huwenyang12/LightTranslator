using LightTranslator.Services.Startup;

namespace LightTranslator.Tests;

public class StartupServiceTests
{
    [Fact]
    public void SetEnabled_WhenTrue_RegistersApplication()
    {
        var backend =
            new FakeStartupRegistrationBackend();

        var service =
            new StartupService(
                backend,
                () => @"C:\Apps\LightTranslator.exe"
            );

        service.SetEnabled(
            true
        );

        Assert.Equal(
            @"C:\Apps\LightTranslator.exe",
            backend.RegisteredExecutablePath
        );

        Assert.Equal(
            0,
            backend.DisableCount
        );
    }

    [Fact]
    public void SetEnabled_WhenFalse_UnregistersApplication()
    {
        var backend =
            new FakeStartupRegistrationBackend();

        var service =
            new StartupService(
                backend,
                () => @"C:\Apps\LightTranslator.exe"
            );

        service.SetEnabled(
            false
        );

        Assert.Null(
            backend.RegisteredExecutablePath
        );

        Assert.Equal(
            1,
            backend.DisableCount
        );
    }

    private sealed class FakeStartupRegistrationBackend
        : IStartupRegistrationBackend
    {
        public string? RegisteredExecutablePath { get; private set; }

        public int DisableCount { get; private set; }

        public void Enable(
            string executablePath
        )
        {
            RegisteredExecutablePath =
                executablePath;
        }

        public void Disable()
        {
            DisableCount++;
        }
    }
}