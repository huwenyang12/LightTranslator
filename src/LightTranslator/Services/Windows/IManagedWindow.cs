using System.Windows;

namespace LightTranslator.Services.Windows;

public interface IManagedWindow
{
    event EventHandler? Closed;

    double Left { get; set; }

    double Top { get; set; }

    double Width { get; set; }

    double Height { get; set; }

    WindowStartupLocation WindowStartupLocation { get; set; }

    WindowState WindowState { get; set; }

    void Show();

    bool Activate();

    void Close();
}
