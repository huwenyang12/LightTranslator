namespace LightTranslator.Services.Ocr;

public sealed class OcrModelException
    : Exception
{
    private const string PublicMessage =
        "OCR model files are unavailable.";

    public OcrModelException(
        string errorCode,
        Exception? innerException = null
    )
        : base(
            PublicMessage,
            innerException
        )
    {
        ErrorCode =
            errorCode;
    }

    public string ErrorCode
    {
        get;
    }
}
