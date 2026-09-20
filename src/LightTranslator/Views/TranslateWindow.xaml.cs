using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LightTranslator.Services.Clipboard;
using LightTranslator.ViewModels;
using LightTranslator.Services.Windows;
using LightTranslator.Services.Settings;

namespace LightTranslator.Views;

public partial class TranslateWindow
    : Window, IManagedWindow
{
    private readonly TranslateViewModel _viewModel;
    private readonly ITextLanguageSettingsPersistence? _languagePersistence;
    private readonly IClipboardService _clipboardService;
    private readonly Action<Window, string> _showClipboardError;

    private CancellationTokenSource? _copyFeedbackCancellation;

    public TranslateWindow(
        TranslateViewModel viewModel,
        ITextLanguageSettingsPersistence? languagePersistence = null
    ) : this(
        viewModel,
        languagePersistence,
        new ClipboardService(),
        (owner, message) =>
            System.Windows.MessageBox.Show(
                owner,
                message,
                "语桥",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            )
    )
    {
    }

    internal TranslateWindow(
        TranslateViewModel viewModel,
        ITextLanguageSettingsPersistence? languagePersistence,
        IClipboardService clipboardService,
        Action<Window, string> showClipboardError
    )
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(showClipboardError);

        InitializeComponent();

        _viewModel =
            viewModel;

        _languagePersistence =
            languagePersistence;

        _clipboardService =
            clipboardService;

        _showClipboardError =
            showClipboardError;

        DataContext =
            viewModel;

        _viewModel.PropertyChanged +=
            OnViewModelPropertyChanged;

        Loaded +=
            OnLoaded;

        SourceInitialized +=
            OnSourceInitialized;

        PreviewKeyDown +=
            OnPreviewKeyDown;

        SourceTextBox.PreviewKeyDown +=
            OnSourceTextBoxPreviewKeyDown;

        Closed +=
            OnClosed;
    }

    private void OnSourceInitialized(
        object? sender,
        EventArgs e
    )
    {
        WindowBackdropService.TryApply(
            this,
            WindowBackdropKind.TransientAcrylic
        );
    }

    private async void OnClosed(
        object? sender,
        EventArgs e
    )
    {
        _viewModel.PropertyChanged -=
            OnViewModelPropertyChanged;

        CancelCopyFeedback(
            resetVisual: false
        );

        if (_languagePersistence is null)
        {
            return;
        }

        await _languagePersistence.SaveAsync(
            _viewModel.SourceLanguage,
            _viewModel.TargetLanguage
        );
    }

    private void OnLoaded(
        object sender,
        RoutedEventArgs e
    )
    {
        SourceTextBox.Focus();
    }

    private void OnPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnSwapLanguagesClick(
        object sender,
        RoutedEventArgs e
    )
    {
        _viewModel.SwapLanguages();
    }


    private async void OnCopyTranslationClick(
        object sender,
        RoutedEventArgs e
    )
    {
        if (
            !TryCopyTranslation() ||
            string.IsNullOrWhiteSpace(
                _viewModel.TranslatedText
            )
        )
        {
            return;
        }

        await ShowCopyFeedbackAsync();
    }

    private async Task ShowCopyFeedbackAsync()
    {
        CancelCopyFeedback(
            resetVisual: false
        );

        var cancellation =
            new CancellationTokenSource();

        _copyFeedbackCancellation = cancellation;

        CopyTranslationButton.Content = "✓ 已复制";
        AutomationProperties.SetName(
            CopyTranslationButton,
            "已复制翻译结果"
        );
        CopyTranslationButton.SetResourceReference(
            System.Windows.Controls.Control.ForegroundProperty,
            "Brush.Accent"
        );
        BeginCopyFeedbackAnimation(
            "Storyboard.CopyFeedback.In"
        );
        RaiseCopyFeedbackAutomationEvent();

        try
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(1000),
                cancellation.Token
            );

            if (SystemParameters.ClientAreaAnimation)
            {
                BeginCopyFeedbackAnimation(
                    "Storyboard.CopyFeedback.Out"
                );

                await Task.Delay(
                    TimeSpan.FromMilliseconds(140),
                    cancellation.Token
                );
            }

            ResetCopyFeedbackVisual();

            if (SystemParameters.ClientAreaAnimation)
            {
                BeginCopyFeedbackAnimation(
                    "Storyboard.CopyFeedback.In"
                );
            }
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (
                ReferenceEquals(
                    _copyFeedbackCancellation,
                    cancellation
                )
            )
            {
                _copyFeedbackCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void BeginCopyFeedbackAnimation(
        string resourceKey
    )
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var storyboard =
            (Storyboard)FindResource(resourceKey);

        storyboard.Begin(
            this,
            HandoffBehavior.SnapshotAndReplace,
            isControllable: true
        );
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName !=
            nameof(TranslateViewModel.TranslatedText)
        )
        {
            return;
        }

        if (Dispatcher.CheckAccess())
        {
            CancelCopyFeedback(
                resetVisual: true
            );
            return;
        }

        if (!Dispatcher.HasShutdownStarted)
        {
            Dispatcher.BeginInvoke(
                () => CancelCopyFeedback(
                    resetVisual: true
                )
            );
        }
    }

    private void CancelCopyFeedback(
        bool resetVisual
    )
    {
        var cancellation =
            _copyFeedbackCancellation;

        _copyFeedbackCancellation = null;
        cancellation?.Cancel();

        StopCopyFeedbackAnimations();

        if (resetVisual)
        {
            ResetCopyFeedbackVisual();
        }
    }

    private void StopCopyFeedbackAnimations()
    {
        foreach (
            var resourceKey in new[]
            {
                "Storyboard.CopyFeedback.In",
                "Storyboard.CopyFeedback.Out"
            }
        )
        {
            if (TryFindResource(resourceKey) is Storyboard storyboard)
            {
                storyboard.Remove(this);
            }
        }

        CopyTranslationButton.Opacity = 1;
    }

    private void ResetCopyFeedbackVisual()
    {
        CopyTranslationButton.Content = "复制";
        AutomationProperties.SetName(
            CopyTranslationButton,
            "复制翻译结果"
        );
        CopyTranslationButton.SetResourceReference(
            System.Windows.Controls.Control.ForegroundProperty,
            "Brush.Text.Secondary"
        );
        CopyTranslationButton.Opacity = 1;
    }

    private void RaiseCopyFeedbackAutomationEvent()
    {
        var peer =
            UIElementAutomationPeer.CreatePeerForElement(
                CopyTranslationButton
            ) ??
            UIElementAutomationPeer.FromElement(
                CopyTranslationButton
            );

        peer?.RaiseAutomationEvent(
            AutomationEvents.LiveRegionChanged
        );
    }

    private bool TryCopyTranslation()
    {
        var translatedText =
            _viewModel.TranslatedText;

        if (string.IsNullOrWhiteSpace(translatedText))
        {
            return true;
        }

        if (_clipboardService.TrySetText(translatedText))
        {
            return true;
        }

        _showClipboardError(
            this,
            "复制失败，请重试。"
        );

        return false;
    }

    private void OnTranslationSurfaceMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e
    )
    {
        if (
            e.ChangedButton != MouseButton.Left ||
            e.ButtonState != MouseButtonState.Pressed ||
            e.OriginalSource is not DependencyObject source ||
            !WindowDragHitTest.CanStartDrag(source)
        )
        {
            return;
        }

        DragMove();
        e.Handled = true;
    }

    private void OnSourceTextBoxPreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (
            e.Key != Key.Enter &&
            e.Key != Key.Return
        )
        {
            return;
        }

        var isShiftPressed =
            Keyboard.Modifiers.HasFlag(
                ModifierKeys.Shift
            );

        if (isShiftPressed)
        {
            e.Handled = true;

            var selectionStart =
                SourceTextBox.SelectionStart;

            var selectionLength =
                SourceTextBox.SelectionLength;

            var currentText =
                SourceTextBox.Text;

            var newText =
                currentText
                    .Remove(
                        selectionStart,
                        selectionLength
                    )
                    .Insert(
                        selectionStart,
                        Environment.NewLine
                    );

            SourceTextBox.Text = newText;

            SourceTextBox.CaretIndex =
                selectionStart +
                Environment.NewLine.Length;

            return;
        }

        e.Handled = true;

        if (!TryCopyTranslation())
        {
            return;
        }

        Close();
    }
}
