using System.Text;

namespace LightTranslator.Services.Ocr;

internal sealed record OcrRecognitionResult(
    string Text,
    double Confidence
);

internal sealed class CtcTextDecoder
{
    private readonly IReadOnlyList<string> _tokens;

    public CtcTextDecoder(
        IReadOnlyList<string> tokens
    )
    {
        ArgumentNullException.ThrowIfNull(
            tokens
        );

        if (tokens.Count == 0)
        {
            throw new ArgumentException(
                "At least the blank token is required.",
                nameof(tokens)
            );
        }

        _tokens = tokens;
    }

    public OcrRecognitionResult Decode(
        float[,,] logits
    )
    {
        ArgumentNullException.ThrowIfNull(
            logits
        );

        if (logits.GetLength(
                0
            ) != 1)
        {
            throw new ArgumentException(
                "A single recognition batch is required.",
                nameof(logits)
            );
        }

        var stepCount =
            logits.GetLength(
                1
            );

        var classCount =
            logits.GetLength(
                2
            );

        if (classCount == 0)
        {
            return
                new OcrRecognitionResult(
                    string.Empty,
                    0
                );
        }

        var text =
            new StringBuilder();

        double confidenceSum = 0;
        var emittedCount = 0;
        var previousIndex = -1;

        for (var step = 0;
             step < stepCount;
             step++)
        {
            var bestIndex = 0;
            var bestLogit =
                logits[
                    0,
                    step,
                    0
                ];

            for (var classIndex = 1;
                 classIndex < classCount;
                 classIndex++)
            {
                var candidate =
                    logits[
                        0,
                        step,
                        classIndex
                    ];

                if (candidate > bestLogit)
                {
                    bestLogit = candidate;
                    bestIndex = classIndex;
                }
            }

            if (bestIndex != 0 &&
                bestIndex != previousIndex &&
                bestIndex < _tokens.Count)
            {
                text.Append(
                    _tokens[
                        bestIndex
                    ]
                );

                confidenceSum +=
                    TokenProbability(
                        logits,
                        step,
                        bestLogit
                    );

                emittedCount++;
            }

            previousIndex = bestIndex;
        }

        return
            new OcrRecognitionResult(
                text.ToString(),
                emittedCount == 0
                    ? 0
                    : confidenceSum /
                      emittedCount
            );
    }

    private static double TokenProbability(
        float[,,] scores,
        int step,
        float maximum
    )
    {
        double scoreSum = 0;
        var containsOnlyProbabilities = true;

        for (var classIndex = 0;
             classIndex <
             scores.GetLength(
                 2
             );
             classIndex++)
        {
            var score =
                scores[
                    0,
                    step,
                    classIndex
                ];

            if (score < 0 ||
                score > 1)
            {
                containsOnlyProbabilities = false;
            }

            scoreSum += score;
        }

        if (containsOnlyProbabilities &&
            Math.Abs(
                scoreSum - 1d
            ) <= 0.05d)
        {
            return maximum;
        }

        double denominator = 0;

        for (var classIndex = 0;
             classIndex <
             scores.GetLength(
                 2
             );
             classIndex++)
        {
            denominator +=
                Math.Exp(
                    scores[
                        0,
                        step,
                        classIndex
                    ] -
                    maximum
                );
        }

        return
            denominator == 0
                ? 0
                : 1d /
                  denominator;
    }
}
