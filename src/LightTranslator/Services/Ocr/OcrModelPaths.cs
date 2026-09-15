namespace LightTranslator.Services.Ocr;

public sealed record OcrModelPaths(
    string DetectionModelPath,
    string RecognitionModelPath,
    string DictionaryPath
);
