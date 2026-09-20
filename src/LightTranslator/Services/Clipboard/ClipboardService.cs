using System.Runtime.InteropServices;

namespace LightTranslator.Services.Clipboard;

internal sealed class ClipboardService
    : IClipboardService
{
    private const int ClipboardCannotOpen =
        unchecked((int)0x800401D0);

    private const int DefaultMaxAttempts = 15;
    private const int DefaultRetryDelayMilliseconds = 100;

    private readonly Action<string> _setText;
    private readonly Action<int> _delay;
    private readonly int _maxAttempts;
    private readonly int _retryDelayMilliseconds;

    internal ClipboardService()
        : this(
            RegisterClipboardText,
            System.Windows.Clipboard.Flush,
            Thread.Sleep,
            maxAttempts: DefaultMaxAttempts,
            retryDelayMilliseconds: DefaultRetryDelayMilliseconds
        )
    {
    }

    internal ClipboardService(
        Action<string> setText,
        Action<int> delay
    ) : this(
            setText,
            delay,
            maxAttempts: DefaultMaxAttempts,
            retryDelayMilliseconds: DefaultRetryDelayMilliseconds
        )
    {
    }

    internal ClipboardService(
        Action<string, bool> registerText,
        Action flush,
        Action<int> delay,
        int maxAttempts,
        int retryDelayMilliseconds
    ) : this(
            text => SetTextWithBestEffortFlush(
                text,
                registerText,
                flush
            ),
            delay,
            maxAttempts,
            retryDelayMilliseconds
        )
    {
        ArgumentNullException.ThrowIfNull(registerText);
        ArgumentNullException.ThrowIfNull(flush);
    }

    internal ClipboardService(
        Action<string> setText,
        Action<int> delay,
        int maxAttempts,
        int retryDelayMilliseconds
    )
    {
        ArgumentNullException.ThrowIfNull(setText);
        ArgumentNullException.ThrowIfNull(delay);

        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts)
            );
        }

        if (retryDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelayMilliseconds)
            );
        }

        _setText = setText;
        _delay = delay;
        _maxAttempts = maxAttempts;
        _retryDelayMilliseconds = retryDelayMilliseconds;
    }

    public bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                _setText(text);
                return true;
            }
            catch (COMException exception)
                when (
                    exception.ErrorCode == ClipboardCannotOpen &&
                    attempt < _maxAttempts
                )
            {
                _delay(_retryDelayMilliseconds);
            }
            catch (COMException exception)
                when (exception.ErrorCode == ClipboardCannotOpen)
            {
                return false;
            }
        }

        return false;
    }

    private static void RegisterClipboardText(
        string text,
        bool copy
    )
    {
        var dataObject =
            new System.Windows.DataObject();

        dataObject.SetText(
            text,
            System.Windows.TextDataFormat.UnicodeText
        );

        System.Windows.Clipboard.SetDataObject(
            dataObject,
            copy
        );
    }

    private static void SetTextWithBestEffortFlush(
        string text,
        Action<string, bool> registerText,
        Action flush
    )
    {
        registerText(
            text,
            false
        );

        try
        {
            flush();
        }
        catch (COMException exception)
            when (exception.ErrorCode == ClipboardCannotOpen)
        {
        }
    }
}
