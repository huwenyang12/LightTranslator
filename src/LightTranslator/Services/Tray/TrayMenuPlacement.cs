namespace LightTranslator.Services.Tray;

internal static class TrayMenuPlacement
{
    internal static System.Windows.Point Calculate(
        double cursorX,
        double cursorY,
        double menuWidth,
        double menuHeight,
        System.Windows.Rect workingArea,
        double margin
    )
    {
        var minimumX = workingArea.Left + margin;
        var minimumY = workingArea.Top + margin;
        var maximumX = Math.Max(
            minimumX,
            workingArea.Right - menuWidth - margin
        );
        var maximumY = Math.Max(
            minimumY,
            workingArea.Bottom - menuHeight - margin
        );

        return new System.Windows.Point(
            Math.Clamp(
                cursorX - menuWidth,
                minimumX,
                maximumX
            ),
            Math.Clamp(
                cursorY - menuHeight,
                minimumY,
                maximumY
            )
        );
    }
}
