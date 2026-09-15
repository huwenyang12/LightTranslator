using LightTranslator.Views;

namespace LightTranslator.Tests;

public class ScreenshotTranslationWindowTests
{
    [Fact]
    public void Window_ShowsPhase2Notice()
    {
        Exception? exception =
            null;

        var thread =
            new Thread(
                () =>
                {
                    ScreenshotTranslationWindow? window =
                        null;

                    try
                    {
                        window =
                            new ScreenshotTranslationWindow();

                        var notice =
                            window.FindName(
                                "PhaseNoticeTextBlock"
                            ) as System.Windows.Controls.TextBlock;

                        Assert.NotNull(
                            notice
                        );

                        Assert.Equal(
                            "截图翻译将在下一阶段启用",
                            notice.Text
                        );
                    }
                    catch (Exception ex)
                    {
                        exception =
                            ex;
                    }
                    finally
                    {
                        window?.Close();
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );
    }
}