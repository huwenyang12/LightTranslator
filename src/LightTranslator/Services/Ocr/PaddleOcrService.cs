using System.Windows.Media.Imaging;
using LightTranslator.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace LightTranslator.Services.Ocr;

public sealed class PaddleOcrService
    : IOcrService,
      IDisposable
{
    private readonly IOcrModelProvider _modelProvider;
    private readonly OcrImagePreprocessor _preprocessor;
    private readonly DbDetectorPostProcessor _detectorPostProcessor;
    private readonly Lazy<RuntimeState> _runtime;
    private bool _disposed;

    public PaddleOcrService(
        IOcrModelProvider modelProvider
    )
    {
        _modelProvider =
            modelProvider ??
            throw new ArgumentNullException(
                nameof(modelProvider)
            );

        _preprocessor =
            new OcrImagePreprocessor();

        _detectorPostProcessor =
            new DbDetectorPostProcessor(
                pixelThreshold: 0.30f,
                boxThreshold: 0.60f,
                unclipRatio: 1.50f
            );

        _runtime =
            new Lazy<RuntimeState>(
                CreateRuntime,
                LazyThreadSafetyMode.ExecutionAndPublication
            );
    }

    public Task<IReadOnlyList<OcrBlock>> RecognizeAsync(
        BitmapSource image,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(
            image
        );

        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return
            Task.Run(
                () =>
                    Recognize(
                        image,
                        cancellationToken
                    ),
                cancellationToken
            );
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_runtime.IsValueCreated)
        {
            _runtime.Value.Dispose();
        }
    }

    private IReadOnlyList<OcrBlock> Recognize(
        BitmapSource image,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtime =
            _runtime.Value;

        var detectorInput =
            _preprocessor.PrepareDetector(
                image
            );

        cancellationToken.ThrowIfCancellationRequested();

        var probabilityMap =
            RunDetector(
                runtime.Detector,
                detectorInput.Tensor
            );

        var boxes =
            _detectorPostProcessor.Process(
                probabilityMap,
                image.PixelWidth,
                image.PixelHeight
            );

        var candidates =
            new List<OcrBlock>();

        foreach (var box in boxes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recognitionInput =
                _preprocessor.PrepareRecognizer(
                    image,
                    box
                );

            var logits =
                RunRecognizer(
                    runtime.Recognizer,
                    recognitionInput.Tensor
                );

            var recognition =
                runtime.Decoder.Decode(
                    logits
                );

            candidates.Add(
                new OcrBlock(
                    $"block-{candidates.Count + 1:0000}",
                    recognition.Text,
                    recognition.Confidence,
                    box
                )
            );
        }

        cancellationToken.ThrowIfCancellationRequested();

        return
            OcrBlock.FilterAndSort(
                candidates,
                minimumConfidence: 0.50
            );
    }

    private RuntimeState CreateRuntime()
    {
        var paths =
            _modelProvider.GetRequiredPaths();

        var detector =
            new InferenceSession(
                paths.DetectionModelPath
            );

        try
        {
            var recognizer =
                new InferenceSession(
                    paths.RecognitionModelPath
                );

            try
            {
                var tokens =
                    new List<string>
                    {
                        string.Empty
                    };

                tokens.AddRange(
                    File.ReadAllLines(
                        paths.DictionaryPath
                    )
                );

                tokens.Add(
                    " "
                );

                return
                    new RuntimeState(
                        detector,
                        recognizer,
                        new CtcTextDecoder(
                            tokens
                        )
                    );
            }
            catch
            {
                recognizer.Dispose();
                throw;
            }
        }
        catch
        {
            detector.Dispose();
            throw;
        }
    }

    private static float[,] RunDetector(
        InferenceSession session,
        DenseTensor<float> tensor
    )
    {
        var inputName =
            session
                .InputMetadata
                .Keys
                .Single();

        using var outputs =
            session.Run(
                new[]
                {
                    NamedOnnxValue.CreateFromTensor(
                        inputName,
                        tensor
                    )
                }
            );

        var output =
            outputs
                .Single()
                .AsTensor<float>();

        var dimensions =
            output
                .Dimensions
                .ToArray();

        if (dimensions.Length < 2)
        {
            throw new InvalidDataException(
                "Detector output must contain an image map."
            );
        }

        var height =
            dimensions[
                ^2
            ];

        var width =
            dimensions[
                ^1
            ];

        var values =
            output.ToArray();

        if (values.Length <
            height *
            width)
        {
            throw new InvalidDataException(
                "Detector output size is invalid."
            );
        }

        var map =
            new float[
                height,
                width
            ];

        var offset =
            values.Length -
            height *
            width;

        for (var row = 0;
             row < height;
             row++)
        {
            for (var column = 0;
                 column < width;
                 column++)
            {
                map[
                    row,
                    column
                ] =
                    values[
                        offset +
                        row *
                        width +
                        column
                    ];
            }
        }

        return map;
    }

    private static float[,,] RunRecognizer(
        InferenceSession session,
        DenseTensor<float> tensor
    )
    {
        var inputName =
            session
                .InputMetadata
                .Keys
                .Single();

        using var outputs =
            session.Run(
                new[]
                {
                    NamedOnnxValue.CreateFromTensor(
                        inputName,
                        tensor
                    )
                }
            );

        var output =
            outputs
                .Single()
                .AsTensor<float>();

        var dimensions =
            output
                .Dimensions
                .ToArray();

        if (dimensions.Length != 3 ||
            dimensions[0] != 1)
        {
            throw new InvalidDataException(
                "Recognizer output must be a rank-3 single batch."
            );
        }

        var stepCount =
            dimensions[1];

        var classCount =
            dimensions[2];

        var values =
            output.ToArray();

        var logits =
            new float[
                1,
                stepCount,
                classCount
            ];

        for (var step = 0;
             step < stepCount;
             step++)
        {
            for (var classIndex = 0;
                 classIndex < classCount;
                 classIndex++)
            {
                logits[
                    0,
                    step,
                    classIndex
                ] =
                    values[
                        step *
                        classCount +
                        classIndex
                    ];
            }
        }

        return logits;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this
        );
    }

    private sealed class RuntimeState
        : IDisposable
    {
        public RuntimeState(
            InferenceSession detector,
            InferenceSession recognizer,
            CtcTextDecoder decoder
        )
        {
            Detector = detector;
            Recognizer = recognizer;
            Decoder = decoder;
        }

        public InferenceSession Detector
        {
            get;
        }

        public InferenceSession Recognizer
        {
            get;
        }

        public CtcTextDecoder Decoder
        {
            get;
        }

        public void Dispose()
        {
            Recognizer.Dispose();
            Detector.Dispose();
        }
    }
}
