using LightTranslator.Services.Hotkeys;

namespace LightTranslator.Tests;

public class Win32HotkeyBackendTests
{

    [Fact]
    public void Unregister_CallsNativeApiWithWindowHandle()
    {
        var nativeApi =
            new FakeHotkeyNativeApi();

        var router =
            new HotkeyMessageRouter();

        var windowHandle =
            new IntPtr(1234);

        var backend =
            new Win32HotkeyBackend(
                windowHandle,
                nativeApi,
                router
            );

        backend.Unregister(
            1
        );

        Assert.Equal(
            windowHandle,
            nativeApi.LastUnregisterWindowHandle
        );

        Assert.Equal(
            1,
            nativeApi.LastUnregisterId
        );
    }

    [Fact]
    public void RouterRaisesHotkey_BackendRaisesHotkeyPressed()
    {
        var nativeApi =
            new FakeHotkeyNativeApi();

        var router =
            new HotkeyMessageRouter();

        var backend =
            new Win32HotkeyBackend(
                new IntPtr(1234),
                nativeApi,
                router
            );

        int? receivedId = null;

        backend.HotkeyPressed +=
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
    public void Register_CallsNativeApiWithWindowHandle()
    {
        var nativeApi =
            new FakeHotkeyNativeApi();

        var router =
            new HotkeyMessageRouter();

        var windowHandle =
            new IntPtr(1234);

        var backend =
            new Win32HotkeyBackend(
                windowHandle,
                nativeApi,
                router
            );

        var result =
            backend.Register(
                id: 1,
                modifiers: 0x0001,
                virtualKey: (uint)'T'
            );

        Assert.True(result);

        Assert.Equal(
            windowHandle,
            nativeApi.LastWindowHandle
        );

        Assert.Equal(
            1,
            nativeApi.LastId
        );

        Assert.Equal(
            0x0001u,
            nativeApi.LastModifiers
        );

        Assert.Equal(
            (uint)'T',
            nativeApi.LastVirtualKey
        );
    }

    private sealed class FakeHotkeyNativeApi
        : IHotkeyNativeApi
    {
        public IntPtr LastWindowHandle { get; private set; }

        public int LastId { get; private set; }

        public uint LastModifiers { get; private set; }

        public uint LastVirtualKey { get; private set; }

        public IntPtr LastUnregisterWindowHandle { get; private set; }

        public int LastUnregisterId { get; private set; }

        public bool RegisterHotKey(
            IntPtr windowHandle,
            int id,
            uint modifiers,
            uint virtualKey
        )
        {
            LastWindowHandle =
                windowHandle;

            LastId =
                id;

            LastModifiers =
                modifiers;

            LastVirtualKey =
                virtualKey;

            return true;
        }

        public bool UnregisterHotKey(
            IntPtr windowHandle,
            int id
        )
        {
            LastUnregisterWindowHandle =
                windowHandle;

            LastUnregisterId =
                id;

            return true;
        }
    }
}