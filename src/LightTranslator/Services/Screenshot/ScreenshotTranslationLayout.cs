using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTranslationLayout
{
    private const double ColumnAlignmentTolerance = 24d;
    private const double BottomSafeArea = 20d;

    public static IReadOnlyDictionary<string, double>
        CalculateAlignedLeftEdges(
            IReadOnlyList<ScreenshotTextRegion> regions,
            double dpiX
        )
    {
        ArgumentNullException.ThrowIfNull(
            regions
        );

        if (dpiX <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    dpiX
                )
            );
        }

        var ordered =
            regions
                .Select(
                    region =>
                        new AlignedRegion(
                            region.Id,
                            region.Bounds.X *
                            96d /
                            dpiX
                        )
                )
                .OrderBy(
                    region =>
                        region.Left
                )
                .ToArray();

        var aligned =
            new Dictionary<string, double>(
                StringComparer.Ordinal
            );

        for (var start = 0;
             start < ordered.Length;)
        {
            var end =
                start +
                1;

            while (
                end < ordered.Length &&
                ordered[end].Left -
                ordered[start].Left <=
                ColumnAlignmentTolerance
            )
            {
                end++;
            }

            var cluster =
                ordered[start..end];

            var middle =
                cluster.Length /
                2;

            var commonLeft =
                cluster.Length % 2 == 0
                    ? (
                        cluster[middle - 1].Left +
                        cluster[middle].Left
                    ) / 2d
                    : cluster[middle].Left;

            foreach (var region in cluster)
            {
                aligned[region.Id] =
                    commonLeft;
            }

            start =
                end;
        }

        return aligned;
    }

    public static double CalculateParagraphGap(
        ScreenshotTextRole currentRole,
        ScreenshotTextRole nextRole,
        double fontSize
    )
    {
        var readableFontSize =
            Math.Max(
                1d,
                fontSize
            );

        if (
            currentRole ==
            ScreenshotTextRole.Title &&
            nextRole ==
            ScreenshotTextRole.Body
        )
        {
            return
                Math.Clamp(
                    readableFontSize *
                    0.5d,
                    7d,
                    14d
                );
        }

        if (
            currentRole ==
            ScreenshotTextRole.Body &&
            nextRole ==
            ScreenshotTextRole.Title
        )
        {
            return
                Math.Clamp(
                    readableFontSize *
                    0.9d,
                    12d,
                    24d
                );
        }

        return
            Math.Clamp(
                readableFontSize *
                0.72d,
                10d,
                18d
            );
    }

    public static double CalculateBottomLimit(
        double windowHeight,
        double sourceBottom
    )
    {
        return
            Math.Max(
                sourceBottom,
                windowHeight -
                BottomSafeArea
            );
    }

    private sealed record AlignedRegion(
        string Id,
        double Left
    );
}
