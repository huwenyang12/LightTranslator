using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightTranslator.Models;
using LightTranslator.Services.ScreenCapture;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Views;

public partial class ScreenshotTranslationWindow
    : Window,
      IScreenshotResultView
{
    private const double MinimumReadableFontSize = 12d;
    private const double MaximumReadableFontSize = 38d;
    private const double SourceCoverExpansion = 2d;
    private const double TranslationLineHeightRatio = 1.28d;
    private const double TargetSourceHeightRatio = 0.75d;

    private readonly ICommand _closeRequestCommand;
    private bool _closeRequestRaised;
    private double _dpiX = 96d;
    private double _dpiY = 96d;
    private BitmapSource? _selectionImage;

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
            message,
            showProgress: true
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

        var visibleRegions =
            regions
                .Where(
                    region =>
                        !string.IsNullOrWhiteSpace(
                            region.TranslatedText
                        ) &&
                        !region.Bounds.IsEmpty
                )
                .OrderBy(
                    region =>
                        region.Bounds.Y
                )
                .ThenBy(
                    region =>
                        region.Bounds.X
                )
                .ToArray();

        var preferredFonts =
            ScreenshotTranslationTypography.CalculatePreferredFontSizes(
                visibleRegions,
                _dpiY
            );

        var alignedLeftEdges =
            ScreenshotTranslationLayout.CalculateAlignedLeftEdges(
                visibleRegions,
                _dpiX
            );

        for (var index = 0;
             index < visibleRegions.Length;
             index++)
        {
            var region =
                visibleRegions[index];

            AddTranslationBlock(
                region,
                preferredFonts[region.Id],
                alignedLeftEdges[region.Id],
                visibleRegions
                    .Skip(
                        index +
                        1
                    )
                    .ToArray()
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
            message,
            showProgress: false
        );
    }

    private void ApplySelection(
        CapturedSelection selection
    )
    {
        ArgumentNullException.ThrowIfNull(
            selection
        );

        _selectionImage =
            selection.Image;

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
        double preferredFontSize,
        double alignedLeft,
        IReadOnlyList<ScreenshotTextRegion> followingRegions
    )
    {
        var sourceBounds =
            DpiCoordinateMapper.PixelsToDips(
                region.Bounds,
                _dpiX,
                _dpiY
            );

        var bounds =
            ExpandForSourceCoverage(
                sourceBounds
            );

        var horizontalPadding =
            Math.Clamp(
                sourceBounds.Height * 0.15d,
                4d,
                8d
            );

        var verticalPadding =
            Math.Clamp(
                sourceBounds.Height * 0.08d,
                2d,
                4d
            );

        var padding =
            new Thickness(
                alignedLeft +
                horizontalPadding -
                sourceBounds.Left,
                verticalPadding,
                horizontalPadding,
                verticalPadding
            );

        var translatedText =
            region.TranslatedText!;

        var style =
            _selectionImage is null
                ? ScreenshotBackgroundStyleResolver.Fallback
                : ScreenshotBackgroundStyleResolver.Resolve(
                    _selectionImage,
                    region.Bounds
                );

        var text =
            new TextBlock
            {
                Text =
                    translatedText,
                Foreground =
                    new SolidColorBrush(
                        style.Foreground
                    ),
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

        var maximumContainerHeight =
            CalculateMaximumContainerHeight(
                bounds,
                region.Role,
                Math.Max(
                    MinimumReadableFontSize,
                    preferredFontSize
                ),
                followingRegions
            );

        var availableHeight =
            Math.Max(
                0d,
                maximumContainerHeight -
                padding.Top -
                padding.Bottom
            );

        text.FontSize =
            CalculateTranslationFontSize(
                text,
                preferredFontSize,
                availableWidth,
                availableHeight,
                region.Role ==
                ScreenshotTextRole.Body
                    ? sourceBounds.Height *
                      TargetSourceHeightRatio
                    : 0d
            );

        text.LineHeight =
            text.FontSize *
            TranslationLineHeightRatio;

        text.LineStackingStrategy =
            LineStackingStrategy.BlockLineHeight;

        text.FontWeight =
            region.Role ==
            ScreenshotTextRole.Title
                ? FontWeights.SemiBold
                : FontWeights.Normal;

        text.Measure(
            new System.Windows.Size(
                double.PositiveInfinity,
                double.PositiveInfinity
            )
        );

        var requiresWrapping =
            text.DesiredSize.Width >
            availableWidth;

        text.Measure(
            new System.Windows.Size(
                availableWidth,
                double.PositiveInfinity
            )
        );

        var containerHeight =
            Math.Min(
                maximumContainerHeight,
                Math.Max(
                    bounds.Height,
                    text.DesiredSize.Height +
                    padding.Top +
                    padding.Bottom
                )
            );

        var hasExplicitLineBreak =
            translatedText.Contains(
                '\n'
            ) ||
            translatedText.Contains(
                '\r'
            );

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
                    containerHeight,
                Padding =
                    padding,
                ClipToBounds =
                    true,
                Background =
                    new SolidColorBrush(
                        style.Background
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
        double availableHeight,
        double targetContentHeight
    )
    {
        if (availableWidth <= 0d ||
            availableHeight <= 0d)
        {
            return MinimumReadableFontSize;
        }

        var candidate =
            Math.Clamp(
                preferredFontSize,
                MinimumReadableFontSize,
                MaximumReadableFontSize
            );

        while (candidate > MinimumReadableFontSize)
        {
            text.FontSize =
                candidate;

            text.LineHeight =
                candidate *
                TranslationLineHeightRatio;

            text.LineStackingStrategy =
                LineStackingStrategy.BlockLineHeight;

            text.Measure(
                new System.Windows.Size(
                    availableWidth,
                    double.PositiveInfinity
                )
            );

            if (text.DesiredSize.Height <= availableHeight)
            {
                break;
            }

            candidate =
                Math.Max(
                    MinimumReadableFontSize,
                    candidate - 1d
                );
        }

        var targetHeight =
            Math.Min(
                availableHeight,
                Math.Max(
                    0d,
                    targetContentHeight
                )
            );

        text.FontSize =
            candidate;

        text.LineHeight =
            candidate *
            TranslationLineHeightRatio;

        text.Measure(
            new System.Windows.Size(
                availableWidth,
                double.PositiveInfinity
            )
        );

        var bestCandidate =
            candidate;

        var bestDistance =
            Math.Abs(
                text.DesiredSize.Height -
                targetHeight
            );

        while (
            candidate <
            MaximumReadableFontSize &&
            text.DesiredSize.Height <
            targetHeight
        )
        {
            candidate =
                Math.Min(
                    MaximumReadableFontSize,
                    candidate +
                    1d
                );

            text.FontSize =
                candidate;

            text.LineHeight =
                candidate *
                TranslationLineHeightRatio;

            text.Measure(
                new System.Windows.Size(
                    availableWidth,
                    double.PositiveInfinity
                )
            );

            if (text.DesiredSize.Height > availableHeight)
            {
                break;
            }

            var distance =
                Math.Abs(
                    text.DesiredSize.Height -
                    targetHeight
                );

            if (distance < bestDistance)
            {
                bestCandidate =
                    candidate;

                bestDistance =
                    distance;
            }
        }

        return bestCandidate;
    }

    private double CalculateMaximumContainerHeight(
        Rect currentBounds,
        ScreenshotTextRole currentRole,
        double currentFontSize,
        IReadOnlyList<ScreenshotTextRegion> followingRegions
    )
    {
        var bottomLimit =
            ScreenshotTranslationLayout.CalculateBottomLimit(
                Height,
                currentBounds.Bottom
            );

        var nextTop =
            followingRegions
                .Select(
                    region =>
                        new
                        {
                            Region =
                                region,
                            Bounds =
                                ExpandForSourceCoverage(
                                    DpiCoordinateMapper.PixelsToDips(
                                        region.Bounds,
                                        _dpiX,
                                        _dpiY
                                    )
                                )
                        }
                )
                .Where(
                    item =>
                        item.Bounds.Y >
                        currentBounds.Y &&
                        HorizontallyOverlaps(
                            currentBounds,
                            item.Bounds
                        )
                )
                .Select(
                    item =>
                        item.Bounds.Y -
                        ScreenshotTranslationLayout.CalculateParagraphGap(
                            currentRole,
                            item.Region.Role,
                            currentFontSize
                        )
                )
                .DefaultIfEmpty(
                    bottomLimit
                )
                .Min();

        return
            Math.Max(
                currentBounds.Height,
                nextTop -
                currentBounds.Y
            );
    }

    private Rect ExpandForSourceCoverage(
        Rect bounds
    )
    {
        var left =
            Math.Max(
                0d,
                bounds.Left -
                SourceCoverExpansion
            );

        var top =
            Math.Max(
                0d,
                bounds.Top -
                SourceCoverExpansion
            );

        var right =
            Math.Min(
                Width,
                bounds.Right +
                SourceCoverExpansion
            );

        var bottom =
            Math.Min(
                Height,
                bounds.Bottom +
                SourceCoverExpansion
            );

        return
            new Rect(
                left,
                top,
                Math.Max(
                    0d,
                    right -
                    left
                ),
                Math.Max(
                    0d,
                    bottom -
                    top
                )
            );
    }

    private static bool HorizontallyOverlaps(
        Rect first,
        Rect second
    )
    {
        return
            Math.Min(
                first.Right,
                second.Right
            ) >
            Math.Max(
                first.Left,
                second.Left
            );
    }

    private void ShowStatus(
        string message,
        bool showProgress
    )
    {
        StatusTextBlock.Text =
            message;

        StatusTextBlock.Visibility =
            Visibility.Visible;

        StatusProgressBar.Visibility =
            showProgress
                ? Visibility.Visible
                : Visibility.Collapsed;

        StatusBorder.Visibility =
            Visibility.Visible;
    }

    private void HideStatus()
    {
        StatusTextBlock.Visibility =
            Visibility.Collapsed;

        StatusProgressBar.Visibility =
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
