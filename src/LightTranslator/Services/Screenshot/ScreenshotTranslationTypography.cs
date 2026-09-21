using LightTranslator.Models;
using System.Text.RegularExpressions;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTranslationTypography
{
    private const double MinimumFontSize = 6d;
    private const double MaximumFontSize = 32d;
    private const double PhysicalLineHeightScale = 0.63d;
    private const double BodyNormalizationTolerance = 0.20d;
    private const double MinimumBodyMedianRatio = 0.78d;
    private const double MaximumBodyMedianRatio = 1.08d;
    private const double NumberedSectionMedianRatio = 0.82d;
    private const double MaximumTitleMedianRatio = 1.12d;

    private static readonly Regex NumberedSectionPattern =
        new(
            @"^(?:第\s*\d+\s*(?:段|节|章)|(?:段落|章节|第)?\s*\d+|(?:paragraph|section|chapter)\s*\d+)\s*[：:]?$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant
        );

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
                        PhysicalLineHeightScale,
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

        var minimumBodySize =
            median *
            MinimumBodyMedianRatio;

        var maximumBodySize =
            median *
            MaximumBodyMedianRatio;

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
            else
            {
                sizes[region.Id] =
                    Math.Clamp(
                        sizes[region.Id],
                        minimumBodySize,
                        maximumBodySize
                    );
            }
        }

        foreach (
            var region in
            regions.Where(
                region =>
                    region.Role ==
                    ScreenshotTextRole.Title
            )
        )
        {
            sizes[region.Id] =
                NumberedSectionPattern.IsMatch(
                    region.Text.Trim()
                )
                    ? Math.Clamp(
                        median *
                        NumberedSectionMedianRatio,
                        MinimumFontSize,
                        MaximumFontSize
                    )
                    : Math.Clamp(
                        sizes[region.Id],
                        median,
                        median *
                        MaximumTitleMedianRatio
                    );
        }

        return sizes;
    }
}
