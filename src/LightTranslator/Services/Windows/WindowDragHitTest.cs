using System.Windows;

namespace LightTranslator.Services.Windows;

internal static class WindowDragHitTest
{
    internal static bool CanStartDrag(
        DependencyObject source
    )
    {
        DependencyObject? current = source;

        while (current is not null)
        {
            if (current is Window)
            {
                return true;
            }

            if (current is System.Windows.Controls.Control)
            {
                return false;
            }

            current = GetParent(current);
        }

        return true;
    }

    private static DependencyObject? GetParent(
        DependencyObject current
    )
    {
        if (current is System.Windows.ContentElement contentElement)
        {
            return System.Windows.ContentOperations.GetParent(
                       contentElement
                   )
                   ?? (contentElement as FrameworkContentElement)
                       ?.Parent;
        }

        if (
            current is System.Windows.Media.Visual ||
            current is System.Windows.Media.Media3D.Visual3D
        )
        {
            return System.Windows.Media.VisualTreeHelper.GetParent(
                       current
                   )
                   ?? LogicalTreeHelper.GetParent(current);
        }

        return LogicalTreeHelper.GetParent(current);
    }
}
