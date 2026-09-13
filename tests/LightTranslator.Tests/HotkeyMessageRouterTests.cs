using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class HotkeyMessageRouterTests
{
    [Fact]
    public void ProcessMessage_WhenMessageIsHotkey_RaisesHotkeyPressed()
    {
        var router =
            new HotkeyMessageRouter();

        int? receivedId = null;

        router.HotkeyPressed +=
            id => receivedId = id;

        router.ProcessMessage(
            HotkeyMessageRouter.WmHotkey,
            new IntPtr(1)
        );

        Assert.Equal(
            1,
            receivedId
        );
    }

    [Fact]
    public void ProcessMessage_WhenMessageIsNotHotkey_DoesNothing()
    {
        var router =
            new HotkeyMessageRouter();

        var raisedCount = 0;

        router.HotkeyPressed +=
            _ => raisedCount++;

        router.ProcessMessage(
            0x0001,
            new IntPtr(1)
        );

        Assert.Equal(
            0,
            raisedCount
        );
    }
}