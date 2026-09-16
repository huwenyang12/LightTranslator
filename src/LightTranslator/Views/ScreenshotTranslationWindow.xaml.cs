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
                FontSize =
                    14,
                TextWrapping =
                    TextWrapping.Wrap,
                TextTrimming =
                    TextTrimming.CharacterEllipsis,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var container =
            new Border
            {
                Width =
                    bounds.Width,
                Height =
                    bounds.Height,
                Padding =
                    new Thickness(
                        4,
                        2,
                        4,
                        2
                    ),
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
