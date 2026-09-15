using System.Reflection;
using System.Windows.Forms;
using LightTranslator.Services.Tray;

namespace LightTranslator.Tests;

public class NotifyIconTrayBackendTests
{
    [Fact]
    public void NotifyIconTrayBackend_ImplementsTrayIconBackendContract()
    {
        Assert.True(
            typeof(ITrayIconBackend).IsAssignableFrom(
                typeof(NotifyIconTrayBackend)
            )
        );
    }

    [Fact]
    public void ContextMenu_ContainsRequiredCommandsInOrder()
    {
        using var backend =
            new NotifyIconTrayBackend();

        var contextMenu =
            GetContextMenu(backend);

        var menuTexts =
            contextMenu.Items
                .Cast<ToolStripItem>()
                .Where(item => item is not ToolStripSeparator)
                .Select(item => item.Text)
                .ToArray();

        Assert.Equal(
            new[]
            {
                "文本翻译",
                "截图翻译",
                "设置",
                "退出"
            },
            menuTexts
        );
    }

    [Fact]
    public void TranslationMenuItems_Click_RaiseCorrespondingEvents()
    {
        using var backend =
            new NotifyIconTrayBackend();

        var textTranslationCount = 0;
        var screenshotTranslationCount = 0;

        backend.TextTranslationRequested +=
            () => textTranslationCount++;

        backend.ScreenshotTranslationRequested +=
            () => screenshotTranslationCount++;

        var contextMenu =
            GetContextMenu(backend);

        contextMenu.Items
            .Cast<ToolStripItem>()
            .Single(item => item.Text == "文本翻译")
            .PerformClick();

        contextMenu.Items
            .Cast<ToolStripItem>()
            .Single(item => item.Text == "截图翻译")
            .PerformClick();

        Assert.Equal(
            1,
            textTranslationCount
        );

        Assert.Equal(
            1,
            screenshotTranslationCount
        );
    }

    private static ContextMenuStrip GetContextMenu(
        NotifyIconTrayBackend backend
    )
    {
        var field =
            typeof(NotifyIconTrayBackend).GetField(
                "_contextMenu",
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        Assert.NotNull(field);

        return Assert.IsType<ContextMenuStrip>(
            field.GetValue(backend)
        );
    }
}