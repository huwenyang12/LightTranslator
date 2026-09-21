using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTranslationLayout
{
    private const double ColumnAlignmentTolerance = 24d;
    private const double MinimumAlignedSourceWidth = 16d;
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
                            dpiX,
                            region.Bounds.Width *
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
                CanJoinColumn(
                    ordered,
                    start,
                    end,
                    ordered[end]
                )
            )
            {
                end++;
            }

            var cluster =
                ordered[start..end];

            var commonLeft =
                cluster[^1].Left;

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

    private static bool CanJoinColumn(
        IReadOnlyList<AlignedRegion> ordered,
        int start,
        int end,
        AlignedRegion candidate
    )
    {
        var first =
            ordered[start];

        if (
            candidate.Left -
            first.Left >
            ColumnAlignmentTolerance
        )
        {
            return false;
        }

        var overlap =
            Math.Max(
                0d,
                Math.Min(
                    first.Right,
                    candidate.Right
                ) -
                Math.Max(
                    first.Left,
                    candidate.Left
                )
            );

        if (
            overlap <
            Math.Min(
                first.Width,
                candidate.Width
            ) *
            0.5d
        )
        {
            return false;
        }

        for (var index = start;
             index < end;
             index++)
        {
            var region =
                ordered[index];

            if (
                region.Width -
                (
                    candidate.Left -
                    region.Left
                ) <
                MinimumAlignedSourceWidth
            )
            {
                return false;
            }
        }

        return
            candidate.Width >=
            MinimumAlignedSourceWidth;
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
        double Left,
        double Width
    )
    {
        public double Right =>
            Left +
            Width;
    }
}
