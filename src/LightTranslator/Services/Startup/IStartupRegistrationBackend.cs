namespace LightTranslator.Services.Startup;

public interface IStartupRegistrationBackend
{
    void Enable(
        string executablePath
    );

    void Disable();
}