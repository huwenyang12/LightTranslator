using System.Reflection;
using System.Drawing;
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
    public void TranslationMenuItems_DoNotShowShortcutDisplayText()
    {
        using var backend =
            new NotifyIconTrayBackend();

        var contextMenu =
            GetContextMenu(backend);

        var textTranslationItem =
            Assert.IsType<ToolStripMenuItem>(
                contextMenu.Items
                    .Cast<ToolStripItem>()
                    .Single(item => item.Text == "文本翻译")
            );

        var screenshotTranslationItem =
            Assert.IsType<ToolStripMenuItem>(
                contextMenu.Items
                    .Cast<ToolStripItem>()
                    .Single(item => item.Text == "截图翻译")
            );

        Assert.True(
            string.IsNullOrEmpty(
                textTranslationItem.ShortcutKeyDisplayString
            )
        );

        Assert.True(
            string.IsNullOrEmpty(
                screenshotTranslationItem.ShortcutKeyDisplayString
            )
        );
    }

    [Fact]
    public void ContextMenu_UsesCompactNativeStyling()
    {
        using var backend =
            new NotifyIconTrayBackend();

        var contextMenu =
            GetContextMenu(backend);
        var systemMenuFont =
            Assert.IsType<Font>(SystemFonts.MenuFont);

        Assert.False(contextMenu.ShowImageMargin);
        Assert.Equal(systemMenuFont.Name, contextMenu.Font.Name);
        Assert.Equal(systemMenuFont.Size, contextMenu.Font.Size);
        Assert.Equal(Color.White, contextMenu.BackColor);

        var renderer =
            Assert.IsType<ToolStripProfessionalRenderer>(
                contextMenu.Renderer
            );

        Assert.Equal(
            Color.FromArgb(232, 240, 254),
            renderer.ColorTable.MenuItemSelected
        );

        foreach (
            var item in contextMenu.Items
                .Cast<ToolStripItem>()
                .Where(item => item is ToolStripMenuItem)
        )
        {
            Assert.Equal(new Padding(10, 5, 10, 5), item.Padding);
            Assert.True(item.AutoSize);
            Assert.True(
                item.GetPreferredSize(Size.Empty).Height >= 30
            );
        }
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
