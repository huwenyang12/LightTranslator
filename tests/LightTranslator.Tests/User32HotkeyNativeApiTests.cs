using LightTranslator.Services.Hotkeys;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LightTranslator.Tests;

public class User32HotkeyNativeApiTests
{
    [Fact]
    public void User32HotkeyNativeApi_ImplementsNativeApiContract()
    {
        Assert.True(
            typeof(IHotkeyNativeApi).IsAssignableFrom(
                typeof(User32HotkeyNativeApi)
            )
        );
    }

    [Fact]
    public void NativeMethods_UseCorrectUser32EntryPoints()
    {
        var type =
            typeof(User32HotkeyNativeApi);

        var registerMethod =
            type.GetMethod(
                "RegisterHotKeyNative",
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

        var unregisterMethod =
            type.GetMethod(
                "UnregisterHotKeyNative",
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

        Assert.NotNull(registerMethod);
        Assert.NotNull(unregisterMethod);

        var registerAttribute =
            registerMethod.GetCustomAttribute<DllImportAttribute>();

        var unregisterAttribute =
            unregisterMethod.GetCustomAttribute<DllImportAttribute>();

        Assert.NotNull(registerAttribute);
        Assert.NotNull(unregisterAttribute);

        Assert.Equal(
            "RegisterHotKey",
            registerAttribute.EntryPoint
        );

        Assert.Equal(
            "UnregisterHotKey",
            unregisterAttribute.EntryPoint
        );
    }
}