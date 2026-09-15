using System.IO;

namespace LightTranslator.Services.Ocr;

public sealed class OcrModelProvider
    : IOcrModelProvider
{
    private const string ModelUnavailable =
        "ModelUnavailable";

    private readonly string _baseDirectory;

    public OcrModelProvider()
        : this(
            AppContext.BaseDirectory
        )
    {
    }

    public OcrModelProvider(
        string baseDirectory
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            baseDirectory
        );

        _baseDirectory =
            Path.GetFullPath(
                baseDirectory
            );
    }

    public OcrModelPaths GetRequiredPaths()
    {
        var paths =
            new OcrModelPaths(
                GetAssetPath(
                    "ppocrv5_mobile_det.onnx"
                ),
                GetAssetPath(
                    "ppocrv5_mobile_rec.onnx"
                ),
                GetAssetPath(
                    "ppocrv5_dict.txt"
                )
            );

        try
        {
            ValidateReadableNonEmptyFile(
                paths.DetectionModelPath
            );

            ValidateReadableNonEmptyFile(
                paths.RecognitionModelPath
            );

            ValidateReadableNonEmptyFile(
                paths.DictionaryPath
            );

            return
                paths;
        }
        catch (Exception exception)
            when (
                exception is IOException or
                UnauthorizedAccessException
            )
        {
            throw new OcrModelException(
                ModelUnavailable,
                exception
            );
        }
    }

    private string GetAssetPath(
        string fileName
    )
    {
        return
            Path.GetFullPath(
                Path.Combine(
                    _baseDirectory,
                    "Assets",
                    "Ocr",
                    fileName
                )
            );
    }

    private static void ValidateReadableNonEmptyFile(
        string path
    )
    {
        using var stream =
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

        if (stream.Length == 0)
        {
            throw new InvalidDataException(
                "OCR model asset is empty."
            );
        }
    }
}
