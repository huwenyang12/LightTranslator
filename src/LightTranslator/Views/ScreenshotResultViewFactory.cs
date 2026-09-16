using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Views;

public sealed class ScreenshotResultViewFactory
    : IScreenshotResultViewFactory
{
    public IScreenshotResultView Create(
        CapturedSelection selection
    )
    {
        ArgumentNullException.ThrowIfNull(
            selection
        );

        return
            new ScreenshotResultView(
                new ScreenshotTranslationWindow(
                    selection
                )
            );
    }

    private sealed class ScreenshotResultView
        : IScreenshotResultView
    {
        private readonly ScreenshotTranslationWindow _window;

        public ScreenshotResultView(
            ScreenshotTranslationWindow window
        )
        {
            _window =
                window;
        }

        public event EventHandler? CloseRequested
        {
            add =>
                _window.CloseRequested +=
                    value;

            remove =>
                _window.CloseRequested -=
                    value;
        }

        public void ShowLoading(
            CapturedSelection selection,
            string message
        )
        {
            _window.ShowLoading(
                selection,
                message
            );

            EnsureShown();
        }

        public void ShowResults(
            IReadOnlyList<OcrBlock> blocks
        )
        {
            _window.ShowResults(
                blocks
            );

            EnsureShown();
        }

        public void ShowMessage(
            string message
        )
        {
            _window.ShowMessage(
                message
            );

            EnsureShown();
        }

        public void Close()
        {
            _window.Close();
        }

        private void EnsureShown()
        {
            if (!_window.IsVisible)
            {
                _window.Show();
            }
        }
    }
}
