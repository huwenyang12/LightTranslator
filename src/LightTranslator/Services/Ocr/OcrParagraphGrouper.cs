using LightTranslator.Models;

namespace LightTranslator.Services.Ocr;

public static class OcrParagraphGrouper
{
    private const double MaximumLineGapRatio =
        1.5d;

    public static IReadOnlyList<OcrBlock> Group(
        IEnumerable<OcrBlock> blocks
    )
    {
        ArgumentNullException.ThrowIfNull(
            blocks
        );

        var ordered =
            blocks
                .Where(
                    block =>
                        !string.IsNullOrWhiteSpace(
                            block.Text
                        ) &&
                        !block.Bounds.IsEmpty
                )
                .OrderBy(
                    block => block.Bounds.Y
                )
                .ThenBy(
                    block => block.Bounds.X
                )
                .ToArray();

        if (ordered.Length == 0)
        {
            return Array.Empty<OcrBlock>();
        }

        var paragraphs =
            new List<List<OcrBlock>>();

        var current =
            new List<OcrBlock>
            {
                ordered[0]
            };

        for (var index = 1;
             index < ordered.Length;
             index++)
        {
            var next =
                ordered[index];

            var previous =
                current[^1];

            if (ShouldStartNewParagraph(
                    previous,
                    next
                ))
            {
                paragraphs.Add(
                    current
                );

                current =
                    new List<OcrBlock>();
            }

            current.Add(
                next
            );
        }

        paragraphs.Add(
            current
        );

        return
            paragraphs
                .Select(
                    (paragraph, index) =>
                        CreateParagraph(
                            paragraph,
                            index + 1
                        )
                )
                .ToArray();
    }

    private static bool ShouldStartNewParagraph(
        OcrBlock previous,
        OcrBlock next
    )
    {
        var previousBottom =
            previous.Bounds.Y +
            previous.Bounds.Height;

        var verticalGap =
            Math.Max(
                0,
                next.Bounds.Y -
                previousBottom
            );

        var referenceHeight =
            Math.Max(
                previous.Bounds.Height,
                next.Bounds.Height
            );

        return
            verticalGap >
            referenceHeight *
            MaximumLineGapRatio;
    }

    private static OcrBlock CreateParagraph(
        IReadOnlyList<OcrBlock> blocks,
        int paragraphNumber
    )
    {
        var left =
            blocks.Min(
                block => block.Bounds.X
            );

        var top =
            blocks.Min(
                block => block.Bounds.Y
            );

        var right =
            blocks.Max(
                block =>
                    block.Bounds.X +
                    block.Bounds.Width
            );

        var bottom =
            blocks.Max(
                block =>
                    block.Bounds.Y +
                    block.Bounds.Height
            );

        return
            new OcrBlock(
                $"paragraph-{paragraphNumber:0000}",
                string.Join(
                    " ",
                    blocks.Select(
                        block =>
                            block.Text.Trim()
                    )
                ),
                blocks.Average(
                    block => block.Confidence
                ),
                new PixelRect(
                    left,
                    top,
                    right - left,
                    bottom - top
                )
            );
    }
}
