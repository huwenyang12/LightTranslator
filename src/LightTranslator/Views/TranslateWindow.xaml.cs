using System.Windows;
using System.Windows.Input;
using LightTranslator.ViewModels;
using LightTranslator.Services.Windows;
using LightTranslator.Services.Settings;

namespace LightTranslator.Views;

public partial class TranslateWindow
    : Window, IManagedWindow
{
    private readonly TranslateViewModel _viewModel;
    private readonly ITextLanguageSettingsPersistence? _languagePersistence;

    public TranslateWindow(
        TranslateViewModel viewModel,
        ITextLanguageSettingsPersistence? languagePersistence = null
    )
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        _languagePersistence =
            languagePersistence;

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
    

    private void OnCopyTranslationClick(
        object sender,
        RoutedEventArgs e
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                _viewModel.TranslatedText
            )
        )
        {
            return;
        }

        System.Windows.Clipboard.SetText(
            _viewModel.TranslatedText
        );
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

        if (
            !string.IsNullOrWhiteSpace(
                _viewModel.TranslatedText
            )
        )
        {
            System.Windows.Clipboard.SetText(
                _viewModel.TranslatedText
            );
        }

        Close();
    }
}
