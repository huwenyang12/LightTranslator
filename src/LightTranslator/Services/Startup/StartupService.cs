namespace LightTranslator.Services.Startup;

public sealed class StartupService
{
    private readonly IStartupRegistrationBackend _backend;

    private readonly Func<string> _executablePathProvider;

    public StartupService(
        IStartupRegistrationBackend backend,
        Func<string> executablePathProvider
    )
    {
        _backend =
            backend;

        _executablePathProvider =
            executablePathProvider;
    }

    public void SetEnabled(
        bool enabled
    )
    {
        if (enabled)
        {
            _backend.Enable(
                _executablePathProvider()
            );

            return;
        }

        _backend.Disable();
    }
}