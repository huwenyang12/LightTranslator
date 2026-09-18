using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTranslationTypography
{
    private const double MinimumFontSize = 6d;
    private const double MaximumFontSize = 32d;
    private const double BodyNormalizationTolerance = 0.20d;

    public static IReadOnlyDictionary<string, double>
        CalculatePreferredFontSizes(
            IReadOnlyList<ScreenshotTextRegion> regions,
            double dpiY
        )
    {
        ArgumentNullException.ThrowIfNull(
            regions
        );

        var sizes =
            regions.ToDictionary(
                region =>
                    region.Id,
                region =>
                    Math.Clamp(
                        region.SourceLineHeight *
                        96d /
                        dpiY *
                        0.80d,
                        MinimumFontSize,
                        MaximumFontSize
                    )
            );

        var bodySizes =
            regions
                .Where(
                    region =>
                        region.Role ==
                        ScreenshotTextRole.Body
                )
                .Select(
                    region =>
                        sizes[region.Id]
                )
                .OrderBy(
                    size =>
                        size
                )
                .ToArray();

        if (bodySizes.Length == 0)
        {
            return sizes;
        }

        var middle =
            bodySizes.Length /
            2;

        var median =
            bodySizes.Length % 2 == 0
                ? (
                    bodySizes[middle - 1] +
                    bodySizes[middle]
                ) / 2d
                : bodySizes[middle];

        var tolerance =
            median *
            BodyNormalizationTolerance;

        foreach (
            var region in
            regions.Where(
                region =>
                    region.Role ==
                    ScreenshotTextRole.Body
            )
        )
        {
            if (
                Math.Abs(
                    sizes[region.Id] -
                    median
                ) <= tolerance
            )
            {
                sizes[region.Id] =
                    median;
            }
        }

        return sizes;
    }
}
