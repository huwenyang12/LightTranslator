# Phase 2B Structure-Aware Screenshot Translation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve title/body structure and source-scale typography in screenshot translation while keeping translated text inside OCR bounds and avoiding any additional model or network call.

**Architecture:** Add a screenshot-specific `ScreenshotTextRegion` model and a pure `ScreenshotTextRegionAnalyzer` between OCR and translation. Keep `IScreenshotTextTranslator` unchanged by adapting regions back to temporary `OcrBlock` items for the existing single batch request, then render regions with DPI-aware font normalization, dynamic padding, alignment rules, and lightweight source-background sampling.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-16-phase2b-structure-aware-layout-design.md`

## Global Constraints

- `OcrBlock` remains raw OCR data; do not add title/body or rendering fields to it.
- `ScreenshotTextRegion.Bounds` and `SourceLineHeight` use physical screenshot pixels.
- Convert `SourceLineHeight` to DIP with `SourceLineHeight * 96 / dpiY` before font calculation.
- Title/body detection uses geometry only; DeepSeek never classifies structure.
- Keep `IScreenshotTextTranslator` unchanged and keep one batch translation network request per screenshot.
- Translation containers remain fixed to OCR-derived bounds and clip at those bounds.
- Font fitting starts from source-scale typography and may shrink to 6 DIP; never below 6 DIP.
- Similar body text normalizes around the screenshot body-font median; titles keep their own larger estimate.
- Single-line translations are vertically centered; wrapped or explicit-newline translations are top aligned.
- Padding is dynamic using `horizontal = clamp(height * 0.12, 1, 4)` and `vertical = clamp(height * 0.06, 0, 2)` in DIP.
- Flat backgrounds use sampled opaque source color and black/white contrast text; complex or unsampleable backgrounds use `ARGB(235,17,24,39)` with white text.
- Do not add OCR passes, DeepSeek calls, ONNX layout/background models, screenshot disk writes, or new NuGet dependencies.
- Preserve existing cancellation, privacy-safe logging, API-key errors, network errors, DPI coordinate mapping, close bindings, and Phase 2B fixed-bounds behavior.

---

### Task 1: Add screenshot text regions and preserve body-line geometry

**Files:**
- Create: `src/LightTranslator/Models/ScreenshotTextRegion.cs`
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs`

**Interfaces:**
- Consumes: filtered/sorted `IReadOnlyList<OcrBlock>` values in physical-pixel coordinates.
- Produces: `IReadOnlyList<ScreenshotTextRegion>` from `ScreenshotTextRegionAnalyzer.Analyze(IReadOnlyList<OcrBlock> blocks)`.

- [ ] **Step 1: Write the RED body-grouping test**

Create `ScreenshotTextRegionAnalyzerTests.cs` with this first test:

```csharp
using LightTranslator.Models;
using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotTextRegionAnalyzerTests
{
    [Fact]
    public void Analyze_GroupsBodyLinesInReadingOrderAndKeepsMedianLineHeight()
    {
        var blocks =
            new[]
            {
                new OcrBlock(
                    "line-3",
                    "third",
                    0.93,
                    new PixelRect(20, 70, 220, 25)
                ),
                new OcrBlock(
                    "line-1",
                    "first",
                    0.95,
                    new PixelRect(20, 10, 200, 24)
                ),
                new OcrBlock(
                    "line-2",
                    "second",
                    0.94,
                    new PixelRect(20, 40, 240, 26)
                )
            };

        var region =
            Assert.Single(
                ScreenshotTextRegionAnalyzer.Analyze(blocks)
            );

        Assert.Equal("region-0001", region.Id);
        Assert.Equal("first second third", region.Text);
        Assert.Equal(ScreenshotTextRole.Body, region.Role);
        Assert.Equal(25d, region.SourceLineHeight, 6);
        Assert.Equal(new PixelRect(20, 10, 240, 85), region.Bounds);
        Assert.Equal(0.94d, region.Confidence, 6);
    }
}
```

- [ ] **Step 2: Run the test and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests.Analyze_GroupsBodyLinesInReadingOrderAndKeepsMedianLineHeight"
```

Expected: compile/test failure because `ScreenshotTextRegion`, `ScreenshotTextRole`, and `ScreenshotTextRegionAnalyzer` do not exist yet.

- [ ] **Step 3: Add the region model**

Create `ScreenshotTextRegion.cs`:

```csharp
namespace LightTranslator.Models;

public enum ScreenshotTextRole
{
    Body,
    Title
}

public sealed record ScreenshotTextRegion(
    string Id,
    string Text,
    double Confidence,
    PixelRect Bounds,
    double SourceLineHeight,
    ScreenshotTextRole Role,
    string? TranslatedText = null
);
```

- [ ] **Step 4: Add the minimum analyzer implementation for body grouping**

Create `ScreenshotTextRegionAnalyzer.cs` with a deterministic proximity cluster and median-height calculation:

```csharp
using LightTranslator.Models;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTextRegionAnalyzer
{
    private const double MaximumLineGapRatio = 1.5d;

    public static IReadOnlyList<ScreenshotTextRegion> Analyze(
        IReadOnlyList<OcrBlock> blocks
    )
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var ordered =
            blocks
                .Where(
                    block =>
                        !string.IsNullOrWhiteSpace(block.Text) &&
                        !block.Bounds.IsEmpty
                )
                .OrderBy(block => block.Bounds.Y)
                .ThenBy(block => block.Bounds.X)
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

        for (var index = 1; index < ordered.Length; index++)
        {
            var previous = current[^1];
            var next = ordered[index];

            var previousBottom =
                previous.Bounds.Y + previous.Bounds.Height;

            var gap =
                Math.Max(
                    0,
                    next.Bounds.Y - previousBottom
                );

            var referenceHeight =
                Math.Max(
                    previous.Bounds.Height,
                    next.Bounds.Height
                );

            if (gap > referenceHeight * MaximumLineGapRatio)
            {
                clusters.Add(current);
                current = new List<OcrBlock>();
            }

            current.Add(next);
        }

        clusters.Add(current);

        return
            clusters
                .Select(
                    (cluster, index) =>
                        CreateRegion(
                            cluster,
                            index + 1,
                            ScreenshotTextRole.Body
                        )
                )
                .ToArray();
    }

    private static ScreenshotTextRegion CreateRegion(
        IReadOnlyList<OcrBlock> lines,
        int number,
        ScreenshotTextRole role
    )
    {
        var left = lines.Min(line => line.Bounds.X);
        var top = lines.Min(line => line.Bounds.Y);
        var right = lines.Max(line => line.Bounds.X + line.Bounds.Width);
        var bottom = lines.Max(line => line.Bounds.Y + line.Bounds.Height);

        return
            new ScreenshotTextRegion(
                $"region-{number:0000}",
                string.Join(" ", lines.Select(line => line.Text.Trim())),
                lines.Average(line => line.Confidence),
                new PixelRect(left, top, right - left, bottom - top),
                Median(lines.Select(line => (double)line.Bounds.Height)),
                role
            );
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        var middle = ordered.Length / 2;

        return
            ordered.Length % 2 == 1
                ? ordered[middle]
                : (ordered[middle - 1] + ordered[middle]) / 2d;
    }
}
```

- [ ] **Step 5: Run the focused analyzer test**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests.Analyze_GroupsBodyLinesInReadingOrderAndKeepsMedianLineHeight"
```

Expected: PASS.

- [ ] **Step 6: Commit Task 1**

```powershell
git add `
  src/LightTranslator/Models/ScreenshotTextRegion.cs `
  src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs `
  tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs

git commit -m "feat: preserve screenshot text region geometry"
```

---

### Task 2: Add conservative title detection without misclassifying UI text

**Files:**
- Modify: `src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs`

**Interfaces:**
- Consumes: one proximity cluster of OCR lines.
- Produces: either one `Body` region, or a separate first-line `Title` region followed by a `Body` region.

- [ ] **Step 1: Add three RED title-detection tests**

Append tests equivalent to:

```csharp
[Fact]
public void Analyze_SeparatesLargerShortTitleFromFollowingBodyLines()
{
    var regions =
        ScreenshotTextRegionAnalyzer.Analyze(
            new[]
            {
                new OcrBlock(
                    "title",
                    "Paragraph 1",
                    0.98,
                    new PixelRect(20, 10, 150, 40)
                ),
                new OcrBlock(
                    "body-1",
                    "Learning a new language is rewarding.",
                    0.96,
                    new PixelRect(20, 56, 420, 24)
                ),
                new OcrBlock(
                    "body-2",
                    "It opens doors to new cultures.",
                    0.95,
                    new PixelRect(20, 86, 440, 24)
                )
            }
        );

    Assert.Equal(2, regions.Count);
    Assert.Equal(ScreenshotTextRole.Title, regions[0].Role);
    Assert.Equal("Paragraph 1", regions[0].Text);
    Assert.Equal(40d, regions[0].SourceLineHeight, 6);
    Assert.Equal(ScreenshotTextRole.Body, regions[1].Role);
    Assert.Equal(
        "Learning a new language is rewarding. It opens doors to new cultures.",
        regions[1].Text
    );
    Assert.Equal(24d, regions[1].SourceLineHeight, 6);
}

[Fact]
public void Analyze_DoesNotClassifyIsolatedLargeLabelAsTitle()
{
    var region =
        Assert.Single(
            ScreenshotTextRegionAnalyzer.Analyze(
                new[]
                {
                    new OcrBlock(
                        "label",
                        "VICTORY",
                        0.99,
                        new PixelRect(20, 10, 200, 42)
                    )
                }
            )
        );

    Assert.Equal(ScreenshotTextRole.Body, region.Role);
}

[Fact]
public void Analyze_DoesNotClassifySameHeightShortLineAsTitle()
{
    var region =
        Assert.Single(
            ScreenshotTextRegionAnalyzer.Analyze(
                new[]
                {
                    new OcrBlock(
                        "line-1",
                        "Status",
                        0.98,
                        new PixelRect(20, 10, 100, 24)
                    ),
                    new OcrBlock(
                        "line-2",
                        "Connection is stable.",
                        0.96,
                        new PixelRect(20, 40, 300, 24)
                    ),
                    new OcrBlock(
                        "line-3",
                        "No action is required.",
                        0.95,
                        new PixelRect(20, 70, 320, 24)
                    )
                }
            )
        );

    Assert.Equal(ScreenshotTextRole.Body, region.Role);
}
```

- [ ] **Step 2: Run the title slice and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests"
```

Expected: the positive title test fails because the analyzer currently emits one body region.

- [ ] **Step 3: Implement conservative first-line title splitting inside each proximity cluster**

Before emitting each cluster, evaluate only its first line as a possible title. Require at least three lines in the cluster. Use the remaining lines as the body evidence:

```csharp
private static bool IsTitleCandidate(
    OcrBlock candidate,
    IReadOnlyList<OcrBlock> following
)
{
    if (following.Count < 2 ||
        candidate.Text.Trim().Length > 60)
    {
        return false;
    }

    var bodyMedianHeight =
        Median(
            following.Select(
                line => (double)line.Bounds.Height
            )
        );

    if (bodyMedianHeight <= 0d ||
        candidate.Bounds.Height < bodyMedianHeight * 1.30d)
    {
        return false;
    }

    if (following.Any(
            line =>
                Math.Abs(line.Bounds.Height - bodyMedianHeight) /
                bodyMedianHeight > 0.20d
        ))
    {
        return false;
    }

    var bodyLeft =
        following.Min(line => line.Bounds.X);

    var bodyRight =
        following.Max(
            line => line.Bounds.X + line.Bounds.Width
        );

    var bodyWidth =
        bodyRight - bodyLeft;

    var alignmentTolerance =
        Math.Max(
            12d,
            bodyWidth * 0.08d
        );

    if (Math.Abs(candidate.Bounds.X - bodyLeft) > alignmentTolerance)
    {
        return false;
    }

    var gap =
        Math.Max(
            0,
            following[0].Bounds.Y -
            (candidate.Bounds.Y + candidate.Bounds.Height)
        );

    return gap <= bodyMedianHeight * 1.5d;
}
```

When the test passes, emit the candidate line as a `Title` region and all remaining cluster lines as one `Body` region. Otherwise emit the whole cluster as one `Body` region. Assign `region-0001`, `region-0002`, etc. in final reading order after splitting.

- [ ] **Step 4: Run all analyzer tests**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests"
```

Expected: all analyzer tests PASS.

- [ ] **Step 5: Commit Task 2**

```powershell
git add `
  src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs `
  tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs

git commit -m "feat: detect screenshot titles conservatively"
```

---

### Task 3: Move the screenshot pipeline to structure-aware regions while keeping one translator call

**Files:**
- Modify: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationCoordinator.cs`
- Modify: `src/LightTranslator/Services/Screenshot/IScreenshotResultView.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationCoordinatorTests.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`
- Delete after migration: `src/LightTranslator/Services/Ocr/OcrParagraphGrouper.cs`
- Delete after migration: `tests/LightTranslator.Tests/OcrParagraphGrouperTests.cs`

**Interfaces:**
- Keeps: `IScreenshotTextTranslator.TranslateAsync(IReadOnlyList<OcrBlock>, ...)` unchanged.
- Changes: `IScreenshotResultView.ShowResults(IReadOnlyList<ScreenshotTextRegion> regions)`.
- Coordinator adapts each region to a temporary `OcrBlock`, calls the translator once, reattaches translated text by region ID, then renders regions.

- [ ] **Step 1: Make the existing coordinator success test RED on the new structure behavior without changing interfaces yet**

Change its OCR fixture to a title plus two body lines and add a translator call counter. Assert that the translator receives two items in one call:

```csharp
Assert.Equal(1, fixture.Translator.CallCount);
Assert.Equal(2, fixture.Translator.LastBlocks.Count);
Assert.Equal("Paragraph 1", fixture.Translator.LastBlocks[0].Text);
Assert.Equal(
    "Learning is rewarding. Practice every day.",
    fixture.Translator.LastBlocks[1].Text
);
```

Set translator results with `region-0001` and `region-0002` keys.

- [ ] **Step 2: Run the coordinator success test and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests.Toggle_CompletesCaptureOcrTranslationAndRendering"
```

Expected: FAIL because the current coordinator still calls `OcrParagraphGrouper` and sends the old paragraph grouping.

- [ ] **Step 3: Replace the old paragraph grouping call with region analysis and temporary translator blocks**

Use this flow in `ScreenshotTranslationCoordinator.RunAsync`:

```csharp
var regions =
    ScreenshotTextRegionAnalyzer.Analyze(blocks);

var translationBlocks =
    regions
        .Select(
            region =>
                new OcrBlock(
                    region.Id,
                    region.Text,
                    region.Confidence,
                    region.Bounds
                )
        )
        .ToArray();

var translations =
    await _textTranslator.TranslateAsync(
        translationBlocks,
        settings.ScreenshotSourceLanguage,
        settings.ScreenshotTargetLanguage,
        cancellationToken
    );
```

Reattach by ID:

```csharp
var translatedRegions =
    regions
        .Select(
            region =>
                translations.TryGetValue(region.Id, out var text) &&
                !string.IsNullOrWhiteSpace(text)
                    ? region with { TranslatedText = text }
                    : region
        )
        .Where(
            region =>
                !string.IsNullOrWhiteSpace(region.TranslatedText)
        )
        .ToArray();
```

For this RED/GREEN step only, adapt `translatedRegions` back to `OcrBlock` for the still-old result view so the coordinator behavior can turn green before the interface refactor.

- [ ] **Step 4: Run the coordinator success test and verify GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests.Toggle_CompletesCaptureOcrTranslationAndRendering"
```

Expected: PASS and translator `CallCount == 1`.

- [ ] **Step 5: Refactor the result-view interface to carry `ScreenshotTextRegion` without changing visible behavior**

Change:

```csharp
void ShowResults(
    IReadOnlyList<ScreenshotTextRegion> regions
);
```

Update the coordinator to pass `translatedRegions` directly. Update the fake result view to store `IReadOnlyList<ScreenshotTextRegion>`. Update `ScreenshotTranslationWindow.ShowResults`/`AddTranslationBlock` parameter types to `ScreenshotTextRegion` while continuing to use the same `Bounds`, `TranslatedText`, fixed size, shrink-to-fit, and clipping behavior already covered by the window regression tests.

Add assertions to the coordinator success test that rendered items preserve roles and source line heights:

```csharp
Assert.Equal(ScreenshotTextRole.Title, rendered[0].Role);
Assert.Equal(40d, rendered[0].SourceLineHeight, 6);
Assert.Equal(ScreenshotTextRole.Body, rendered[1].Role);
Assert.Equal(24d, rendered[1].SourceLineHeight, 6);
```

- [ ] **Step 6: Run coordinator and screenshot-window regressions after the interface refactor**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: all selected tests PASS.

- [ ] **Step 7: Retire the obsolete grouping path**

Delete `OcrParagraphGrouper.cs` and `OcrParagraphGrouperTests.cs` only after the new analyzer tests and coordinator tests are green. Search the branch for `OcrParagraphGrouper` and require zero production references.

```powershell
git grep "OcrParagraphGrouper"
```

Expected after deletion: no output.

- [ ] **Step 8: Commit Task 3**

```powershell
git add -A

git commit -m "refactor: carry screenshot structure through translation"
```

---

### Task 4: Add DPI-aware typography normalization, dynamic padding, and alignment

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationTypography.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotTranslationTypographyTests.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`

**Interfaces:**
- Produces: `ScreenshotTranslationTypography.CalculatePreferredFontSizes(regions, dpiY)` returning a dictionary from region ID to normalized starting font size in DIP.
- Renderer then applies existing measured shrink-to-fit from that starting size.

- [ ] **Step 1: Write RED typography tests for body normalization, title preservation, and DPI conversion**

Create tests using regions such as:

```csharp
[Fact]
public void CalculatePreferredFontSizes_NormalizesSimilarBodyTextButKeepsLargeTitle()
{
    var regions =
        new[]
        {
            new ScreenshotTextRegion(
                "title",
                "Title",
                0.98,
                new PixelRect(0, 0, 200, 50),
                50,
                ScreenshotTextRole.Title
            ),
            new ScreenshotTextRegion(
                "body-1",
                "Body one",
                0.95,
                new PixelRect(0, 60, 300, 30),
                30,
                ScreenshotTextRole.Body
            ),
            new ScreenshotTextRegion(
                "body-2",
                "Body two",
                0.95,
                new PixelRect(0, 100, 300, 32),
                32,
                ScreenshotTextRole.Body
            )
        };

    var sizes =
        ScreenshotTranslationTypography.CalculatePreferredFontSizes(
            regions,
            120d
        );

    Assert.Equal(sizes["body-1"], sizes["body-2"], 6);
    Assert.True(sizes["title"] > sizes["body-1"]);
}

[Theory]
[InlineData(96d, 24d, 19.2d)]
[InlineData(120d, 30d, 19.2d)]
[InlineData(144d, 36d, 19.2d)]
public void CalculatePreferredFontSizes_ConvertsPhysicalLineHeightToDip(
    double dpiY,
    double sourceLineHeight,
    double expected
)
{
    var region =
        new ScreenshotTextRegion(
            "body",
            "Body",
            0.95,
            new PixelRect(0, 0, 300, 60),
            sourceLineHeight,
            ScreenshotTextRole.Body
        );

    var sizes =
        ScreenshotTranslationTypography.CalculatePreferredFontSizes(
            new[] { region },
            dpiY
        );

    Assert.Equal(expected, sizes["body"], 6);
}
```

- [ ] **Step 2: Run typography tests and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests"
```

Expected: compile/test failure because the helper does not exist.

- [ ] **Step 3: Implement the pure typography helper**

Use:

```csharp
sourceDip = region.SourceLineHeight * 96d / dpiY;
preferred = Math.Clamp(sourceDip * 0.80d, 6d, 32d);
```

Calculate the median preferred size among `Body` regions. Any body preferred size within `20%` of the median uses the exact median; outliers keep their own preferred size. Titles always keep their own preferred size.

- [ ] **Step 4: Run typography tests and verify GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests"
```

Expected: PASS.

- [ ] **Step 5: Add RED WPF assertions for dynamic padding and vertical alignment**

Extend `ScreenshotTranslationWindowTests` so one short translation in a wide region asserts `VerticalAlignment.Center`, while an explicit-newline or forced-wrap translation asserts `VerticalAlignment.Top`.

Also render a small and large region and assert their `Border.Padding` values differ according to:

```text
horizontal = clamp(mappedHeight * 0.12, 1, 4)
vertical   = clamp(mappedHeight * 0.06, 0, 2)
```

At 120 DPI, a 15 px source height maps to 12 DIP and should produce approximately `Thickness(1.44, 0.72, 1.44, 0.72)`. A 50 px source height maps to 40 DIP and should clamp to `Thickness(4, 2, 4, 2)`.

Add a font assertion that a large title can begin above the old 18 DIP maximum when it fits.

- [ ] **Step 6: Run the window/layout slice and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: failures because the window still uses fixed padding, center alignment, and the old 18 DIP source-height start.

- [ ] **Step 7: Integrate normalized starting fonts and dynamic layout into the window**

In `ShowResults`, calculate preferred sizes once:

```csharp
var preferredFonts =
    ScreenshotTranslationTypography.CalculatePreferredFontSizes(
        regions,
        _dpiY
    );
```

For each region:

1. Map its bounds to DIP with `DpiCoordinateMapper`.
2. Calculate padding from mapped region height.
3. Use `preferredFonts[region.Id]` as the starting font.
4. Measure/shrink by 1 DIP until text fits or reaches 6 DIP.
5. Decide multi-line status after the final font size: explicit newline OR unconstrained desired text width greater than usable width means top alignment; otherwise center.
6. Keep fixed `Width`, fixed `Height`, `TextWrapping.Wrap`, `TextTrimming.None`, and `ClipToBounds = true`.

Change the fit helper signature so it accepts the preferred start explicitly instead of recalculating from merged region height:

```csharp
private static double CalculateTranslationFontSize(
    TextBlock text,
    double preferredFontSize,
    double availableWidth,
    double availableHeight
)
```

- [ ] **Step 8: Run typography + window + DPI regressions**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests|FullyQualifiedName~DpiCoordinateMapperTests"
```

Expected: all selected tests PASS.

- [ ] **Step 9: Commit Task 4**

```powershell
git add `
  src/LightTranslator/Services/Screenshot/ScreenshotTranslationTypography.cs `
  src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationTypographyTests.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs

git commit -m "feat: preserve source-scale screenshot typography"
```

---

### Task 5: Adapt translation-block colors to flat source backgrounds

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotBackgroundStyleResolver.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotBackgroundStyleResolverTests.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`

**Interfaces:**
- Produces: `ScreenshotBackgroundStyleResolver.Resolve(BitmapSource image, PixelRect bounds)` returning background/foreground WPF colors.
- Uses physical-pixel region bounds directly against the cropped screenshot bitmap.

- [ ] **Step 1: Write RED background-style tests**

Cover four cases with small `WriteableBitmap` fixtures in a supported 24/32-bit BGR(A) format:

```csharp
[Fact]
public void Resolve_FlatLightBackgroundUsesSampledColorAndDarkText()
{
    var image = CreateSolidBitmap(100, 60, 245, 245, 245);

    var style =
        ScreenshotBackgroundStyleResolver.Resolve(
            image,
            new PixelRect(10, 10, 60, 30)
        );

    Assert.Equal(Color.FromArgb(255, 245, 245, 245), style.Background);
    Assert.Equal(Colors.Black, style.Foreground);
}

[Fact]
public void Resolve_FlatDarkBackgroundUsesSampledColorAndWhiteText()
{
    var image = CreateSolidBitmap(100, 60, 20, 20, 20);

    var style =
        ScreenshotBackgroundStyleResolver.Resolve(
            image,
            new PixelRect(10, 10, 60, 30)
        );

    Assert.Equal(Color.FromArgb(255, 20, 20, 20), style.Background);
    Assert.Equal(Colors.White, style.Foreground);
}

[Fact]
public void Resolve_HighVarianceBackgroundUsesOpaqueDarkFallback()
{
    var image = CreateCheckerboardBitmap(100, 60);

    var style =
        ScreenshotBackgroundStyleResolver.Resolve(
            image,
            new PixelRect(10, 10, 60, 30)
        );

    Assert.Equal(Color.FromArgb(235, 17, 24, 39), style.Background);
    Assert.Equal(Colors.White, style.Foreground);
}

[Fact]
public void Resolve_OutOfBoundsRegionUsesSafeFallback()
{
    var image = CreateSolidBitmap(20, 20, 245, 245, 245);

    var style =
        ScreenshotBackgroundStyleResolver.Resolve(
            image,
            new PixelRect(30, 30, 10, 10)
        );

    Assert.Equal(Color.FromArgb(235, 17, 24, 39), style.Background);
    Assert.Equal(Colors.White, style.Foreground);
}
```

- [ ] **Step 2: Run resolver tests and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests"
```

Expected: compile/test failure because the resolver does not exist.

- [ ] **Step 3: Implement the resolver**

Create:

```csharp
public readonly record struct ScreenshotBackgroundStyle(
    Color Background,
    Color Foreground
);
```

and a static resolver with these rules:

- Reject empty/out-of-bounds regions to fallback.
- Support `PixelFormats.Bgr24`, `PixelFormats.Bgr32`, `PixelFormats.Bgra32`, and `PixelFormats.Pbgra32`; unsupported formats fall back safely.
- Generate 5 evenly spaced X positions and 5 Y positions inside the region, staying one pixel inside edges when dimensions permit.
- Sample at most 25 pixels with `BitmapSource.CopyPixels`.
- Median R/G/B is calculated independently.
- Per-sample luma is `(0.2126 * R + 0.7152 * G + 0.0722 * B)` on the `0..255` scale for standard deviation.
- Flat only when luma standard deviation `<= 12` and every RGB channel range `<= 24`.
- Flat style: `Color.FromArgb(255, medianR, medianG, medianB)` and black foreground when normalized median luma `>= 0.55`, otherwise white.
- Fallback: `Color.FromArgb(235, 17, 24, 39)` and white foreground.

Keep this helper deterministic and free of UI-window state.

- [ ] **Step 4: Run resolver tests and verify GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests"
```

Expected: PASS.

- [ ] **Step 5: Add a RED integration assertion in the screenshot window**

Use a flat light test selection, call `ShowResults`, and assert the rendered `Border.Background` resolves to the sampled light color and the child `TextBlock.Foreground` is black. Add a complex checkerboard selection assertion for the `ARGB(235,17,24,39)` fallback.

- [ ] **Step 6: Run window tests and verify RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationWindowTests"
```

Expected: background assertions fail because the window still uses the old universal dark block.

- [ ] **Step 7: Retain the selection bitmap and apply resolved styles per region**

Add a field:

```csharp
private BitmapSource? _selectionImage;
```

Set it in `ApplySelection` from `selection.Image`. In `AddTranslationBlock`, call:

```csharp
var style =
    _selectionImage is null
        ? ScreenshotBackgroundStyleResolver.Fallback
        : ScreenshotBackgroundStyleResolver.Resolve(
            _selectionImage,
            region.Bounds
        );
```

Use an opaque sampled `SolidColorBrush(style.Background)` and `SolidColorBrush(style.Foreground)`. Do not save or mutate the screenshot.

- [ ] **Step 8: Run all screenshot rendering tests**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: all selected tests PASS.

- [ ] **Step 9: Commit Task 5**

```powershell
git add `
  src/LightTranslator/Services/Screenshot/ScreenshotBackgroundStyleResolver.cs `
  src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs `
  tests/LightTranslator.Tests/ScreenshotBackgroundStyleResolverTests.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs

git commit -m "feat: adapt screenshot translation backgrounds"
```

---

### Task 6: Full regression, build, and manual comparison

**Files:**
- No new production changes expected.
- Verify all changes from Tasks 1-5.

**Interfaces:**
- Consumes: the complete structure-aware screenshot translation pipeline.
- Produces: evidence that Phase 2B remains stable and that the manual result improves over the current screenshots.

- [ ] **Step 1: Run all Release tests**

```powershell
dotnet test -c Release
```

Expected: 0 failed and 0 skipped tests.

- [ ] **Step 2: Run Release build**

```powershell
dotnet build -c Release
```

Expected: 0 warnings and 0 errors.

- [ ] **Step 3: Check repository consistency**

```powershell
git grep "OcrParagraphGrouper"
git diff --check
git status --short
```

Expected: no `OcrParagraphGrouper` references, no whitespace errors, and a clean working tree after commits.

- [ ] **Step 4: Launch the exact Release build from this branch**

```powershell
Start-Process "D:\PROJECT2\me\LightTranslator\src\LightTranslator\bin\Release\net8.0-windows\Bridgo.exe"
```

- [ ] **Step 5: Repeat the same manual comparison scenario used to expose the current defects**

Verify these concrete outcomes:

```text
1. "Paragraph 1" / "Paragraph 2" are separate title regions when their geometry is clearly larger than following body text.
2. The title is visibly larger than body translation.
3. Normal body translation no longer starts unnecessarily tiny.
4. Long body translation still shrinks and stays inside its original OCR rectangle.
5. Multi-line paragraphs start at the top of their region.
6. Short one-line labels remain vertically centered.
7. White/flat page areas use a light sampled block with dark text instead of a universal dark rectangle.
8. Complex/game/image backgrounds use the near-opaque dark fallback with white text.
9. Original English text is substantially less visible through the overlay than in the previous manual screenshot.
10. Click, Esc, and Alt+Q still close the result overlay.
```

If any item fails, stop and add the smallest focused RED regression test before changing production code.
