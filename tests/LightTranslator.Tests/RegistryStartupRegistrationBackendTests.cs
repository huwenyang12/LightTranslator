using LightTranslator.Services.Startup;

namespace LightTranslator.Tests;

public class RegistryStartupRegistrationBackendTests
{
    [Fact]
    public void RegistryBackend_ImplementsStartupRegistrationContract()
    {
        Assert.True(
            typeof(IStartupRegistrationBackend).IsAssignableFrom(
                typeof(RegistryStartupRegistrationBackend)
            )
        );
    }
}