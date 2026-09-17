using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTextRegionAnalyzer
{
    private const double MaximumLineGapRatio =
        1.5d;

    public static IReadOnlyList<ScreenshotTextRegion> Analyze(
        IReadOnlyList<OcrBlock> blocks
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
            return Array.Empty<ScreenshotTextRegion>();
        }

        var clusters =
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
            var previous =
                current[^1];

            var next =
                ordered[index];

            var previousBottom =
                previous.Bounds.Y +
                previous.Bounds.Height;

            var gap =
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

            if (gap > referenceHeight * MaximumLineGapRatio)
            {
                clusters.Add(
                    current
                );

                current =
                    new List<OcrBlock>();
            }

            current.Add(
                next
            );
        }

        clusters.Add(
            current
        );

        var regions =
            new List<ScreenshotTextRegion>();

        foreach (var cluster in clusters)
        {
            if (
                cluster.Count >= 3 &&
                IsTitleCandidate(
                    cluster[0],
                    cluster.Skip(1).ToArray()
                )
            )
            {
                regions.Add(
                    CreateRegion(
                        new[]
                        {
                            cluster[0]
                        },
                        regions.Count + 1,
                        ScreenshotTextRole.Title
                    )
                );

                regions.Add(
                    CreateRegion(
                        cluster.Skip(1).ToArray(),
                        regions.Count + 1,
                        ScreenshotTextRole.Body
                    )
                );

                continue;
            }

            regions.Add(
                CreateRegion(
                    cluster,
                    regions.Count + 1,
                    ScreenshotTextRole.Body
                )
            );
        }

        return regions;
    }

    private static bool IsTitleCandidate(
        OcrBlock candidate,
        IReadOnlyList<OcrBlock> following
    )
    {
        if (
            following.Count < 2 ||
            candidate.Text.Trim().Length > 60
        )
        {
            return false;
        }

        var bodyMedianHeight =
            Median(
                following.Select(
                    line =>
                        (double)line.Bounds.Height
                )
            );

        if (
            bodyMedianHeight <= 0d ||
            candidate.Bounds.Height <
            bodyMedianHeight * 1.30d
        )
        {
            return false;
        }

        if (
            following.Any(
                line =>
                    Math.Abs(
                        line.Bounds.Height -
                        bodyMedianHeight
                    ) /
                    bodyMedianHeight >
                    0.20d
            )
        )
        {
            return false;
        }

        var bodyLeft =
            following.Min(
                line => line.Bounds.X
            );

        var bodyRight =
            following.Max(
                line =>
                    line.Bounds.X +
                    line.Bounds.Width
            );

        var bodyWidth =
            bodyRight - bodyLeft;

        var alignmentTolerance =
            Math.Max(
                12d,
                bodyWidth * 0.08d
            );

        if (
            Math.Abs(
                candidate.Bounds.X -
                bodyLeft
            ) >
            alignmentTolerance
        )
        {
            return false;
        }

        var gap =
            Math.Max(
                0,
                following[0].Bounds.Y -
                (
                    candidate.Bounds.Y +
                    candidate.Bounds.Height
                )
            );

        return
            gap <=
            bodyMedianHeight * 1.5d;
    }

    private static ScreenshotTextRegion CreateRegion(
        IReadOnlyList<OcrBlock> lines,
        int number,
        ScreenshotTextRole role
    )
    {
        var left =
            lines.Min(
                line => line.Bounds.X
            );

        var top =
            lines.Min(
                line => line.Bounds.Y
            );

        var right =
            lines.Max(
                line =>
                    line.Bounds.X +
                    line.Bounds.Width
            );

        var bottom =
            lines.Max(
                line =>
                    line.Bounds.Y +
                    line.Bounds.Height
            );

        return
            new ScreenshotTextRegion(
                $"region-{number:0000}",
                string.Join(
                    " ",
                    lines.Select(
                        line => line.Text.Trim()
                    )
                ),
                lines.Average(
                    line => line.Confidence
                ),
                new PixelRect(
                    left,
                    top,
                    right - left,
                    bottom - top
                ),
                Median(
                    lines.Select(
                        line =>
                            (double)line.Bounds.Height
                    )
                ),
                role
            );
    }

    private static double Median(
        IEnumerable<double> values
    )
    {
        var ordered =
            values
                .OrderBy(
                    value => value
                )
                .ToArray();

        var middle =
            ordered.Length / 2;

        return
            ordered.Length % 2 == 1
                ? ordered[middle]
                : (
                    ordered[middle - 1] +
                    ordered[middle]
                ) / 2d;
    }
}
