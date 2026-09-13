using System.Windows;
using System.Windows.Input;
using LightTranslator.ViewModels;
using LightTranslator.Services.Windows;

namespace LightTranslator.Views;

public partial class TranslateWindow
    : Window, IManagedWindow
{
    private readonly TranslateViewModel _viewModel;

    public TranslateWindow(
        TranslateViewModel viewModel
    )
    {
        InitializeComponent();

        _viewModel = viewModel;

        DataContext = viewModel;

        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;

        SourceTextBox.PreviewKeyDown +=
            OnSourceTextBoxPreviewKeyDown;
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