using LightTranslator.Services.Tray;

namespace LightTranslator.Tests;

public class NotifyIconTrayBackendTests
{
    [Fact]
    public void NotifyIconTrayBackend_ImplementsTrayIconBackendContract()
    {
        Assert.True(
            typeof(ITrayIconBackend).IsAssignableFrom(
                typeof(NotifyIconTrayBackend)
            )
        );
    }
}