namespace LightTranslator.Services.Translation;

public enum TranslationErrorKind
{
    Configuration,
    InvalidApiKey,
    InsufficientBalance,
    RateLimited,
    Timeout,
    Network,
    InvalidResponse,
    Unknown
}

public sealed class TranslationException : Exception
{
    public TranslationErrorKind Kind { get; }

    public TranslationException(
        TranslationErrorKind kind,
        string message,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        Kind = kind;
    }
}