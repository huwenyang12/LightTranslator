namespace LightTranslator.Tests;

public class SmokeTests
{
    [Fact]
    public void ApplicationAssembly_IsLoadable()
    {
        var assembly = typeof(App).Assembly;

        Assert.Equal("LightTranslator", assembly.GetName().Name);
    }
}