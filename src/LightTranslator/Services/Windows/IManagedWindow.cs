namespace LightTranslator.Services.Windows;

public interface IManagedWindow
{
    event EventHandler? Closed;

    void Show();

    void Close();
}