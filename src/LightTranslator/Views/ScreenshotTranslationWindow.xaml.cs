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
    private const double MaximumTranslationFontSize = 18d;

    private static readonly Thickness TranslationPadding =
        new(
            4,
            2,
            4,
            2
        );

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
        IReadOnlyList<OcrBlock> blocks
    )
    {
        ArgumentNullException.ThrowIfNull(
            blocks
        );

        TranslationCanvas.Children.Clear();

        HideStatus();

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(
                    block.TranslatedText
                ) ||
                block.Bounds.IsEmpty)
            {
                continue;
            }

            AddTranslationBlock(
                block
            );
        }
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
        OcrBlock block
    )
    {
        var bounds =
            DpiCoordinateMapper.PixelsToDips(
                block.Bounds,
                _dpiX,
                _dpiY
            );

        var text =
            new TextBlock
            {
                Text =
                    block.TranslatedText,
                Foreground =
                    System.Windows.Media.Brushes.White,
                TextWrapping =
                    TextWrapping.Wrap,
                TextTrimming =
                    TextTrimming.None,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var availableWidth =
            Math.Max(
                0d,
                bounds.Width -
                TranslationPadding.Left -
                TranslationPadding.Right
            );

        var availableHeight =
            Math.Max(
                0d,
                bounds.Height -
                TranslationPadding.Top -
                TranslationPadding.Bottom
            );

        text.FontSize =
            CalculateTranslationFontSize(
                text,
                bounds.Height,
                availableWidth,
                availableHeight
            );

        var container =
            new Border
            {
                Width =
                    bounds.Width,
                Height =
                    bounds.Height,
                Padding =
                    TranslationPadding,
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
        double sourceHeight,
        double availableWidth,
        double availableHeight
    )
    {
        var preferred =
            Math.Clamp(
                sourceHeight * 0.80d,
                MinimumTranslationFontSize,
                MaximumTranslationFontSize
            );

        if (availableWidth <= 0d ||
            availableHeight <= 0d)
        {
            return MinimumTranslationFontSize;
        }

        var candidate =
            preferred;

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
