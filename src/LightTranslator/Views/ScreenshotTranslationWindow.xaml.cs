using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LightTranslator.Models;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Views;

public partial class ScreenshotTranslationWindow
    : Window,
      IScreenshotResultView
{
    private const double MinimumTranslationFontSize = 6d;

    private readonly ICommand _closeRequestCommand;
    private bool _closeRequestRaised;
    private double _dpiX = 96d;
    private double _dpiY = 96d;

    public ScreenshotTranslationWindow()
    {
        InitializeComponent();

        _closeRequestCommand =
            new DelegateCommand(
                RaiseCloseRequested
            );

        ConfigureCloseBindings();
    }

    public ScreenshotTranslationWindow(
        CapturedSelection selection
    )
        : this()
    {
        ApplySelection(
            selection
        );
    }

    public event EventHandler? CloseRequested;

    public void ShowLoading(
        CapturedSelection selection,
        string message
    )
    {
        ApplySelection(
            selection
        );

        TranslationCanvas.Children.Clear();

        ShowStatus(
            message
        );
    }

    public void ShowResults(
        IReadOnlyList<ScreenshotTextRegion> regions
    )
    {
        ArgumentNullException.ThrowIfNull(
            regions
        );

        TranslationCanvas.Children.Clear();

        HideStatus();

        var preferredFonts =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                regions,
                _dpiY
            );

        foreach (var region in regions)
        {
            if (string.IsNullOrWhiteSpace(
                    region.TranslatedText
                ) ||
                region.Bounds.IsEmpty)
            {
                continue;
            }

            AddTranslationBlock(
                region,
                preferredFonts[region.Id]
            );
        }
    }

    public void ShowResults(
        IReadOnlyList<OcrBlock> blocks
    )
    {
        ArgumentNullException.ThrowIfNull(
            blocks
        );

        ShowResults(
            blocks
                .Select(
                    block =>
                        new ScreenshotTextRegion(
                            block.Id,
                            block.Text,
                            block.Confidence,
                            block.Bounds,
                            block.Bounds.Height,
                            ScreenshotTextRole.Body,
                            block.TranslatedText
                        )
                )
                .ToArray()
        );
    }

    public void ShowMessage(
        string message
    )
    {
        TranslationCanvas.Children.Clear();

        ShowStatus(
            message
        );
    }

    private void ApplySelection(
        CapturedSelection selection
    )
    {
        ArgumentNullException.ThrowIfNull(
            selection
        );

        FrozenSelectionImage.Source =
            selection.Image;

        _dpiX =
            selection.DpiX;

        _dpiY =
            selection.DpiY;

        var screenBounds =
            DpiCoordinateMapper.PixelsToDips(
                selection.ScreenBounds,
                selection.DpiX,
                selection.DpiY
            );

        WindowStartupLocation =
            WindowStartupLocation.Manual;

        Left =
            screenBounds.X;

        Top =
            screenBounds.Y;

        Width =
            screenBounds.Width;

        Height =
            screenBounds.Height;
    }

    private void AddTranslationBlock(
        ScreenshotTextRegion region,
        double preferredFontSize
    )
    {
        var bounds =
            DpiCoordinateMapper.PixelsToDips(
                region.Bounds,
                _dpiX,
                _dpiY
            );

        var horizontalPadding =
            Math.Clamp(
                bounds.Height * 0.12d,
                1d,
                4d
            );

        var verticalPadding =
            Math.Clamp(
                bounds.Height * 0.06d,
                0d,
                2d
            );

        var padding =
            new Thickness(
                horizontalPadding,
                verticalPadding,
                horizontalPadding,
                verticalPadding
            );

        var text =
            new TextBlock
            {
                Text =
                    region.TranslatedText,
                Foreground =
                    System.Windows.Media.Brushes.White,
                TextWrapping =
                    TextWrapping.Wrap,
                TextTrimming =
                    TextTrimming.None
            };

        var availableWidth =
            Math.Max(
                0d,
                bounds.Width -
                padding.Left -
                padding.Right
            );

        var availableHeight =
            Math.Max(
                0d,
                bounds.Height -
                padding.Top -
                padding.Bottom
            );

        text.FontSize =
            CalculateTranslationFontSize(
                text,
                preferredFontSize,
                availableWidth,
                availableHeight
            );

        text.Measure(
            new System.Windows.Size(
                double.PositiveInfinity,
                double.PositiveInfinity
            )
        );

        var hasExplicitLineBreak =
            region.TranslatedText.Contains(
                '\n'
            ) ||
            region.TranslatedText.Contains(
                '\r'
            );

        var requiresWrapping =
            text.DesiredSize.Width >
            availableWidth;

        text.VerticalAlignment =
            hasExplicitLineBreak ||
            requiresWrapping
                ? VerticalAlignment.Top
                : VerticalAlignment.Center;

        var container =
            new Border
            {
                Width =
                    bounds.Width,
                Height =
                    bounds.Height,
                Padding =
                    padding,
                ClipToBounds =
                    true,
                Background =
                    new SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(
                            199,
                            17,
                            24,
                            39
                        )
                    ),
                CornerRadius =
                    new CornerRadius(
                        3
                    ),
                Child =
                    text
            };

        Canvas.SetLeft(
            container,
            bounds.X
        );

        Canvas.SetTop(
            container,
            bounds.Y
        );

        TranslationCanvas.Children.Add(
            container
        );
    }

    private static double CalculateTranslationFontSize(
        TextBlock text,
        double preferredFontSize,
        double availableWidth,
        double availableHeight
    )
    {
        if (availableWidth <= 0d ||
            availableHeight <= 0d)
        {
            return MinimumTranslationFontSize;
        }

        var candidate =
            Math.Max(
                MinimumTranslationFontSize,
                preferredFontSize
            );

        while (candidate > MinimumTranslationFontSize)
        {
            text.FontSize =
                candidate;

            text.Measure(
                new System.Windows.Size(
                    availableWidth,
                    double.PositiveInfinity
                )
            );

            if (text.DesiredSize.Height <= availableHeight)
            {
                return candidate;
            }

            candidate =
                Math.Max(
                    MinimumTranslationFontSize,
                    candidate - 1d
                );
        }

        return MinimumTranslationFontSize;
    }

    private void ShowStatus(
        string message
    )
    {
        StatusTextBlock.Text =
            message;

        StatusTextBlock.Visibility =
            Visibility.Visible;

        StatusBorder.Visibility =
            Visibility.Visible;
    }

    private void HideStatus()
    {
        StatusTextBlock.Visibility =
            Visibility.Collapsed;

        StatusBorder.Visibility =
            Visibility.Collapsed;
    }

    private void ConfigureCloseBindings()
    {
        InputBindings.Add(
            new MouseBinding(
                _closeRequestCommand,
                new MouseGesture(
                    MouseAction.LeftClick
                )
            )
        );

        InputBindings.Add(
            new KeyBinding(
                _closeRequestCommand,
                Key.Escape,
                ModifierKeys.None
            )
        );

        InputBindings.Add(
            new KeyBinding(
                _closeRequestCommand,
                Key.Q,
                ModifierKeys.Alt
            )
        );
    }

    private void RaiseCloseRequested()
    {
        if (_closeRequestRaised)
        {
            return;
        }

        _closeRequestRaised =
            true;

        CloseRequested?.Invoke(
            this,
            EventArgs.Empty
        );
    }

    private sealed class DelegateCommand
        : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(
            Action execute
        )
        {
            _execute =
                execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        public bool CanExecute(
            object? parameter
        )
        {
            return true;
        }

        public void Execute(
            object? parameter
        )
        {
            _execute();
        }
    }
}
