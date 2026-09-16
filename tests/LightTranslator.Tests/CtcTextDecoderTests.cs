using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class CtcTextDecoderTests
{
    [Fact]
    public void Decode_CollapsesRepeatsAndBlankTokens()
    {
        var decoder =
            new CtcTextDecoder(
                new[]
                {
                    "",
                    "你",
                    "好"
                }
            );

        var logits =
            CreateLogits(
                classCount: 3,
                0,
                1,
                1,
                0,
                2,
                2,
                0
            );

        var result =
            decoder.Decode(logits);

        Assert.Equal("你好", result.Text);
        Assert.InRange(result.Confidence, 0.99, 1.0);
    }

    [Fact]
    public void Decode_BlankBetweenDuplicatesEmitsTheCharacterTwice()
    {
        var decoder =
            new CtcTextDecoder(
                new[]
                {
                    "",
                    "a"
                }
            );

        var result =
            decoder.Decode(
                CreateLogits(
                    classCount: 2,
                    1,
                    0,
                    1
                )
            );

        Assert.Equal("aa", result.Text);
        Assert.InRange(result.Confidence, 0.99, 1.0);
    }

    [Fact]
    public void Decode_WhenScoresAreProbabilities_UsesMaximumScoreDirectly()
    {
        var decoder =
            new CtcTextDecoder(
                new[]
                {
                    "",
                    "a",
                    "b"
                }
            );

        var probabilities =
            new float[1, 3, 3];

        probabilities[0, 0, 0] = 0.98f;
        probabilities[0, 0, 1] = 0.01f;
        probabilities[0, 0, 2] = 0.01f;

        probabilities[0, 1, 0] = 0.01f;
        probabilities[0, 1, 1] = 0.98f;
        probabilities[0, 1, 2] = 0.01f;

        probabilities[0, 2, 0] = 0.01f;
        probabilities[0, 2, 1] = 0.02f;
        probabilities[0, 2, 2] = 0.97f;

        var result =
            decoder.Decode(
                probabilities
            );

        Assert.Equal("ab", result.Text);
        Assert.InRange(
            result.Confidence,
            0.974,
            0.976
        );
    }

    private static float[,,] CreateLogits(
        int classCount,
        params int[] indexes
    )
    {
        var logits =
            new float[
                1,
                indexes.Length,
                classCount
            ];

        for (var step = 0; step < indexes.Length; step++)
        {
            for (var classIndex = 0;
                 classIndex < classCount;
                 classIndex++)
            {
                logits[0, step, classIndex] = -20f;
            }

            logits[0, step, indexes[step]] = 20f;
        }

        return logits;
    }
}
