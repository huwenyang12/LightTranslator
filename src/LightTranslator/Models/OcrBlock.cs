namespace LightTranslator.Models;

public sealed record OcrBlock(
    string Id,
    string Text,
    double Confidence,
    PixelRect Bounds,
    string? TranslatedText = null
)
{
    public static IReadOnlyList<OcrBlock> FilterAndSort(
        IEnumerable<OcrBlock> blocks,
        double minimumConfidence
    )
    {
        ArgumentNullException.ThrowIfNull(
            blocks
        );

        var candidates =
            blocks
                .Where(
                    block =>
                        block.Confidence >= minimumConfidence &&
                        !string.IsNullOrWhiteSpace(
                            block.Text
                        )
                )
                .OrderBy(
                    block => block.Bounds.Y
                )
                .ThenBy(
                    block => block.Bounds.X
                )
                .ToArray();

        var lines =
            new List<OcrLine>();

        foreach (var block in candidates)
        {
            var line =
                lines.FirstOrDefault(
                    candidate =>
                        candidate.OverlapsVertically(
                            block.Bounds
                        )
                );

            if (line is null)
            {
                lines.Add(
                    new OcrLine(
                        block
                    )
                );

                continue;
            }

            line.Add(
                block
            );
        }

        return
            lines
                .OrderBy(
                    line => line.Top
                )
                .SelectMany(
                    line =>
                        line.Blocks.OrderBy(
                            block => block.Bounds.X
                        )
                )
                .ToArray();
    }

    private sealed class OcrLine
    {
        private int bottom;

        public OcrLine(
            OcrBlock firstBlock
        )
        {
            Blocks =
                new List<OcrBlock>
                {
                    firstBlock
                };

            Top =
                firstBlock.Bounds.Y;

            bottom =
                firstBlock.Bounds.Y +
                firstBlock.Bounds.Height;
        }

        public List<OcrBlock> Blocks
        {
            get;
        }

        public int Top
        {
            get;
            private set;
        }

        public void Add(
            OcrBlock block
        )
        {
            Blocks.Add(
                block
            );

            Top =
                Math.Min(
                    Top,
                    block.Bounds.Y
                );

            bottom =
                Math.Max(
                    bottom,
                    block.Bounds.Y +
                    block.Bounds.Height
                );
        }

        public bool OverlapsVertically(
            PixelRect bounds
        )
        {
            var overlap =
                Math.Min(
                    bottom,
                    bounds.Y +
                    bounds.Height
                ) -
                Math.Max(
                    Top,
                    bounds.Y
                );

            var referenceHeight =
                Math.Min(
                    bottom - Top,
                    bounds.Height
                );

            return
                overlap >=
                Math.Max(
                    1,
                    referenceHeight / 2
                );
        }
    }
}
