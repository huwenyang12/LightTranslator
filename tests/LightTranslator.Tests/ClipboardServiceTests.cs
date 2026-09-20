using System.Runtime.InteropServices;
using LightTranslator.Services.Clipboard;

namespace LightTranslator.Tests;

public sealed class ClipboardServiceTests
{
    private const int ClipboardCannotOpen =
        unchecked((int)0x800401D0);

    [Fact]
    public void TrySetText_WhenClipboardBecomesAvailable_RetriesAndSucceeds()
    {
        var attempts = 0;
        var delays = new List<int>();
        var service =
            new ClipboardService(
                _ =>
                {
                    attempts++;

                    if (attempts < 3)
                    {
                        throw new COMException(
                            "Clipboard busy.",
                            ClipboardCannotOpen
                        );
                    }
                },
                delay => delays.Add(delay),
                maxAttempts: 5,
                retryDelayMilliseconds: 50
            );

        var copied = service.TrySetText("你好");

        Assert.True(copied);
        Assert.Equal(3, attempts);
        Assert.Equal([50, 50], delays);
    }

    [Fact]
    public void TrySetText_WithDefaultPolicy_WhenClipboardIsBusyForHalfSecond_Succeeds()
    {
        var attempts = 0;
        var delays = new List<int>();
        var service =
            new ClipboardService(
                _ =>
                {
                    attempts++;

                    if (attempts < 7)
                    {
                        throw new COMException(
                            "Clipboard busy.",
                            ClipboardCannotOpen
                        );
                    }
                },
                delay => delays.Add(delay)
            );

        var copied = service.TrySetText("你好");

        Assert.True(copied);
        Assert.Equal(7, attempts);
        Assert.Equal(
            [100, 100, 100, 100, 100, 100],
            delays
        );
    }

    [Fact]
    public void TrySetText_WhenTextIsRegisteredButFlushIsContended_ReturnsTrue()
    {
        string? registeredText = null;
        bool? requestedPersistence = null;
        var service =
            new ClipboardService(
                (text, copy) =>
                {
                    registeredText = text;
                    requestedPersistence = copy;
                },
                () => throw new COMException(
                    "Clipboard listener opened the clipboard.",
                    ClipboardCannotOpen
                ),
                _ => { },
                maxAttempts: 5,
                retryDelayMilliseconds: 50
            );

        var copied = service.TrySetText("你好");

        Assert.True(copied);
        Assert.Equal("你好", registeredText);
        Assert.False(requestedPersistence);
    }

    [Fact]
    public void TrySetText_WhenClipboardStaysBusy_ReturnsFalseAfterRetryLimit()
    {
        var attempts = 0;
        var service =
            new ClipboardService(
                _ =>
                {
                    attempts++;
                    throw new COMException(
                        "Clipboard busy.",
                        ClipboardCannotOpen
                    );
                },
                _ => { },
                maxAttempts: 5,
                retryDelayMilliseconds: 50
            );

        var copied = service.TrySetText("你好");

        Assert.False(copied);
        Assert.Equal(5, attempts);
    }

    [Fact]
    public void TrySetText_WhenClipboardThrowsDifferentComError_Rethrows()
    {
        var exception = new COMException("Unexpected.", unchecked((int)0x80004005));
        var service =
            new ClipboardService(
                _ => throw exception,
                _ => { },
                maxAttempts: 5,
                retryDelayMilliseconds: 50
            );

        var thrown = Assert.Throws<COMException>(
            () => service.TrySetText("你好")
        );

        Assert.Same(exception, thrown);
    }
}
