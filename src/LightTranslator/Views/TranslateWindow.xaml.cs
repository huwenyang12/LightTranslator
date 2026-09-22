using System.Windows;
using System.Windows.Input;
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

    private void OnCloseButtonClick(
        object sender,
        RoutedEventArgs e
    )
    {
        Close();
    }

    private void OnMinimizeButtonClick(
        object sender,
        RoutedEventArgs e
    )
    {
        WindowState = WindowState.Minimized;
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
        var key =
            e.Key == Key.ImeProcessed
                ? e.ImeProcessedKey
                : e.Key;

        if (
            key != Key.Enter &&
            key != Key.Return
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
