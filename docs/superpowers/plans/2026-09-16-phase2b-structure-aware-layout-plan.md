# Phase 2B Structure-Aware Screenshot Translation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve title/body structure and source-scale typography in screenshot translation while keeping translated text inside OCR bounds and avoiding any additional model or network call.

**Architecture:** Add `ScreenshotTextRegion` plus a pure `ScreenshotTextRegionAnalyzer` between OCR and translation. Keep `IScreenshotTextTranslator` unchanged by adapting regions to temporary `OcrBlock` items for the existing one-call batch translator, then render regions with DPI-aware font normalization, dynamic padding, alignment rules, and lightweight source-background sampling.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-16-phase2b-structure-aware-layout-design.md`

## Global Constraints

- `OcrBlock` stays raw OCR data; do not add title/body/rendering fields.
- `ScreenshotTextRegion.Bounds` and `SourceLineHeight` are physical screenshot pixels.
- Convert line height to DIP with `SourceLineHeight * 96 / dpiY` before font sizing.
- Title/body classification uses geometry only.
- `IScreenshotTextTranslator` stays unchanged and one screenshot still produces exactly one translator call.
- Translation boxes remain fixed to OCR-derived bounds and clip at those bounds.
- Font fitting never goes below 6 DIP.
- Similar body text normalizes around a screenshot-wide body-font median; titles keep their own larger estimate.
- One-line translations are vertically centered; wrapped/newline translations are top aligned.
- Dynamic padding: `horizontal = clamp(height * 0.12, 1, 4)`, `vertical = clamp(height * 0.06, 0, 2)` in DIP.
- Flat backgrounds use sampled opaque source color plus black/white contrast text; complex/unsampleable backgrounds use `ARGB(235,17,24,39)` plus white text.
- No extra OCR pass, DeepSeek call, ONNX layout/background model, screenshot persistence, or new NuGet dependency.
- Preserve cancellation, privacy-safe logging, error messages, DPI mapping, close bindings, and the already-tested fixed-bounds overflow behavior.

---

### Task 1: Add structure-aware text regions

**Files:**
- Create: `src/LightTranslator/Models/ScreenshotTextRegion.cs`
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs`

**Interfaces:**
- Produces `IReadOnlyList<ScreenshotTextRegion> ScreenshotTextRegionAnalyzer.Analyze(IReadOnlyList<OcrBlock> blocks)`.

- [ ] **Step 1: Write the first failing body-grouping test**

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

- [ ] **Step 2: Run RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests.Analyze_GroupsBodyLinesInReadingOrderAndKeepsMedianLineHeight"
```

Expected: compile/test failure because the new model/analyzer do not exist.

- [ ] **Step 3: Add the region model**

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

- [ ] **Step 4: Add the minimum body-grouping analyzer**

Use the existing paragraph proximity rule (`1.5x` max neighboring line height), reading-order sort `(Y, X)`, merged bounds, average confidence, joined text with spaces, and a median helper:

```csharp
private static double Median(IEnumerable<double> values)
{
    var ordered = values.OrderBy(value => value).ToArray();
    var middle = ordered.Length / 2;

    return
        ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2d;
}
```

Create region IDs as `region-0001`, `region-0002`, ... in final reading order. For this task every region is `Body`.

- [ ] **Step 5: Run GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add `
  src/LightTranslator/Models/ScreenshotTextRegion.cs `
  src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs `
  tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs

git commit -m "feat: preserve screenshot text region geometry"
```

---

### Task 2: Add conservative title detection

**Files:**
- Modify: `src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs`

**Interfaces:**
- A proximity cluster becomes either one `Body` region or one first-line `Title` region plus one `Body` region.

- [ ] **Step 1: Add the positive title RED test**

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
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests.Analyze_SeparatesLargerShortTitleFromFollowingBodyLines"
```

Expected: FAIL because the analyzer emits one body region.

- [ ] **Step 3: Implement the exact conservative title predicate**

Within one proximity cluster, only test the first line as a title and only when at least two following lines exist:

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

    var bodyLeft = following.Min(line => line.Bounds.X);
    var bodyRight =
        following.Max(
            line => line.Bounds.X + line.Bounds.Width
        );

    var alignmentTolerance =
        Math.Max(
            12d,
            (bodyRight - bodyLeft) * 0.08d
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

If true, emit the first line as `Title` and the remaining cluster as `Body`; otherwise emit the entire cluster as `Body`.

- [ ] **Step 4: Add two negative regression tests**

```csharp
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

- [ ] **Step 5: Run all analyzer tests**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTextRegionAnalyzerTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add `
  src/LightTranslator/Services/Screenshot/ScreenshotTextRegionAnalyzer.cs `
  tests/LightTranslator.Tests/ScreenshotTextRegionAnalyzerTests.cs

git commit -m "feat: detect screenshot titles conservatively"
```

---

### Task 3: Carry structure through the existing one-call translation pipeline

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
- Keep `IScreenshotTextTranslator.TranslateAsync(IReadOnlyList<OcrBlock>, ...)` unchanged.
- Final result-view interface becomes `ShowResults(IReadOnlyList<ScreenshotTextRegion> regions)`.

- [ ] **Step 1: Make the coordinator success test RED on structure splitting**

Use this OCR fixture:

```csharp
fixture.Ocr.Blocks =
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
            "Learning is rewarding.",
            0.96,
            new PixelRect(20, 56, 300, 24)
        ),
        new OcrBlock(
            "body-2",
            "Practice every day.",
            0.95,
            new PixelRect(20, 86, 320, 24)
        )
    };

fixture.Translator.Result =
    new Dictionary<string, string>
    {
        ["region-0001"] = "第1段",
        ["region-0002"] = "学习很有收获。每天练习。"
    };
```

Add `CallCount` to the fake translator and increment it at the start of `TranslateAsync`. Assert after rendering:

```csharp
Assert.Equal(1, fixture.Translator.CallCount);
Assert.Equal(2, fixture.Translator.LastBlocks.Count);
Assert.Equal("Paragraph 1", fixture.Translator.LastBlocks[0].Text);
Assert.Equal(
    "Learning is rewarding. Practice every day.",
    fixture.Translator.LastBlocks[1].Text
);
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests.Toggle_CompletesCaptureOcrTranslationAndRendering"
```

Expected: FAIL because the coordinator still uses `OcrParagraphGrouper`.

- [ ] **Step 3: Replace paragraph grouping with analyzer output and adapt regions to temporary translator blocks**

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
```

Call `_textTranslator.TranslateAsync(...)` once with `translationBlocks`, then reattach translation text by ID:

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

For this first GREEN only, map translated regions back to temporary `OcrBlock` values before calling the still-old result view.

- [ ] **Step 4: Run GREEN for the coordinator behavior**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests.Toggle_CompletesCaptureOcrTranslationAndRendering"
```

Expected: PASS and `CallCount == 1`.

- [ ] **Step 5: Refactor the internal result-view interface to regions while behavior stays green**

Change `IScreenshotResultView` to:

```csharp
void ShowResults(
    IReadOnlyList<ScreenshotTextRegion> regions
);
```

Update the fake view property to:

```csharp
public IReadOnlyList<ScreenshotTextRegion> LastResults { get; private set; } =
    Array.Empty<ScreenshotTextRegion>();
```

Update `ScreenshotTranslationWindow.ShowResults` and `AddTranslationBlock` parameter types to `ScreenshotTextRegion`, but keep the already-tested fixed bounds, shrink-to-fit, wrapping, trimming, and clipping behavior unchanged in this refactor.

The coordinator now passes `translatedRegions` directly. Add assertions:

```csharp
Assert.Equal(ScreenshotTextRole.Title, fixture.ResultView.LastResults[0].Role);
Assert.Equal(40d, fixture.ResultView.LastResults[0].SourceLineHeight, 6);
Assert.Equal(ScreenshotTextRole.Body, fixture.ResultView.LastResults[1].Role);
Assert.Equal(24d, fixture.ResultView.LastResults[1].SourceLineHeight, 6);
```

- [ ] **Step 6: Run coordinator/window regressions**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationCoordinatorTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: PASS.

- [ ] **Step 7: Remove the obsolete grouper**

Delete `OcrParagraphGrouper.cs` and `OcrParagraphGrouperTests.cs`, then run:

```powershell
git grep "OcrParagraphGrouper"
```

Expected: no output.

- [ ] **Step 8: Commit**

```powershell
git add -A

git commit -m "refactor: carry screenshot structure through translation"
```

---

### Task 4: Preserve source-scale typography and improve paragraph layout

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationTypography.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotTranslationTypographyTests.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`

**Interfaces:**
- `ScreenshotTranslationTypography.CalculatePreferredFontSizes(IReadOnlyList<ScreenshotTextRegion> regions, double dpiY)` returns starting font sizes in DIP keyed by region ID.

- [ ] **Step 1: Write RED tests for normalization and DPI**

```csharp
[Fact]
public void CalculatePreferredFontSizes_NormalizesSimilarBodiesAndKeepsLargeTitle()
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

- [ ] **Step 2: Run RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests"
```

- [ ] **Step 3: Implement typography helper**

For every region:

```csharp
var sourceDip = region.SourceLineHeight * 96d / dpiY;
var preferred = Math.Clamp(sourceDip * 0.80d, 6d, 32d);
```

Compute the median preferred size of all `Body` regions. If a body preferred size is within `20%` of that median, replace it with the exact median; otherwise keep its own size. Titles never normalize to the body median.

- [ ] **Step 4: Run typography GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests"
```

Expected: PASS.

- [ ] **Step 5: Add RED window assertions for padding, alignment, and >18 DIP title sizing**

Use `ScreenshotTextRegion` fixtures and assert:

```csharp
Assert.Equal(
    new Thickness(1.44, 0.72, 1.44, 0.72),
    smallBorder.Padding
);

Assert.Equal(
    new Thickness(4, 2, 4, 2),
    largeBorder.Padding
);

Assert.Equal(
    VerticalAlignment.Center,
    singleLineText.VerticalAlignment
);

Assert.Equal(
    VerticalAlignment.Top,
    wrappedText.VerticalAlignment
);

Assert.True(titleText.FontSize > 18d);
```

At 120 DPI use a 15 px-high small region (12 DIP mapped height) and a 50 px-high large region (40 DIP mapped height). Give the title enough width/height that its preferred size can fit without shrinking below 18 DIP.

- [ ] **Step 6: Run window RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: failures from fixed padding, always-center alignment, and old 18 DIP start behavior.

- [ ] **Step 7: Integrate typography and layout rules**

In `ShowResults` calculate preferred fonts once:

```csharp
var preferredFonts =
    ScreenshotTranslationTypography.CalculatePreferredFontSizes(
        regions,
        _dpiY
    );
```

For every region:

```csharp
var horizontal =
    Math.Clamp(bounds.Height * 0.12d, 1d, 4d);

var vertical =
    Math.Clamp(bounds.Height * 0.06d, 0d, 2d);

var padding =
    new Thickness(
        horizontal,
        vertical,
        horizontal,
        vertical
    );
```

Change the fit helper to start from the provided preferred size:

```csharp
private static double CalculateTranslationFontSize(
    TextBlock text,
    double preferredFontSize,
    double availableWidth,
    double availableHeight
)
```

Measure/shrink by 1 DIP to a 6 DIP floor. After the final font is chosen, classify visual layout as multiline when either `TranslatedText` contains `\n`/`\r` or unconstrained measured width is greater than available width. Use `Top` for multiline and `Center` otherwise. Keep `TextWrapping.Wrap`, `TextTrimming.None`, fixed bounds, and `ClipToBounds = true`.

- [ ] **Step 8: Run layout regressions**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationTypographyTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests|FullyQualifiedName~DpiCoordinateMapperTests"
```

Expected: PASS.

- [ ] **Step 9: Commit**

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

### Task 5: Adapt overlay colors to source backgrounds

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotBackgroundStyleResolver.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotBackgroundStyleResolverTests.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`

**Interfaces:**
- `ScreenshotBackgroundStyleResolver.Resolve(BitmapSource image, PixelRect bounds)` returns background/foreground colors.
- `ScreenshotBackgroundStyleResolver.Fallback` is the deterministic complex/unsafe fallback.

- [ ] **Step 1: Write RED resolver tests**

Use these four assertions:

```csharp
Assert.Equal(
    Color.FromArgb(255, 245, 245, 245),
    ScreenshotBackgroundStyleResolver.Resolve(
        CreateSolidBitmap(100, 60, 245, 245, 245),
        new PixelRect(10, 10, 60, 30)
    ).Background
);

Assert.Equal(
    Colors.Black,
    ScreenshotBackgroundStyleResolver.Resolve(
        CreateSolidBitmap(100, 60, 245, 245, 245),
        new PixelRect(10, 10, 60, 30)
    ).Foreground
);

Assert.Equal(
    Colors.White,
    ScreenshotBackgroundStyleResolver.Resolve(
        CreateSolidBitmap(100, 60, 20, 20, 20),
        new PixelRect(10, 10, 60, 30)
    ).Foreground
);

Assert.Equal(
    Color.FromArgb(235, 17, 24, 39),
    ScreenshotBackgroundStyleResolver.Resolve(
        CreateCheckerboardBitmap(100, 60),
        new PixelRect(10, 10, 60, 30)
    ).Background
);

Assert.Equal(
    ScreenshotBackgroundStyleResolver.Fallback,
    ScreenshotBackgroundStyleResolver.Resolve(
        CreateSolidBitmap(20, 20, 245, 245, 245),
        new PixelRect(30, 30, 10, 10)
    )
);
```

Define the test bitmap helpers in the same test file so no helper is implicit:

```csharp
private static BitmapSource CreateSolidBitmap(
    int width,
    int height,
    byte red,
    byte green,
    byte blue
)
{
    var pixels = new byte[width * height * 4];

    for (var index = 0; index < pixels.Length; index += 4)
    {
        pixels[index] = blue;
        pixels[index + 1] = green;
        pixels[index + 2] = red;
        pixels[index + 3] = 255;
    }

    var bitmap =
        BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4
        );

    bitmap.Freeze();
    return bitmap;
}

private static BitmapSource CreateCheckerboardBitmap(
    int width,
    int height
)
{
    var pixels = new byte[width * height * 4];

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var value =
                ((x / 5) + (y / 5)) % 2 == 0
                    ? (byte)0
                    : (byte)255;

            var index = (y * width + x) * 4;
            pixels[index] = value;
            pixels[index + 1] = value;
            pixels[index + 2] = value;
            pixels[index + 3] = 255;
        }
    }

    var bitmap =
        BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4
        );

    bitmap.Freeze();
    return bitmap;
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests"
```

- [ ] **Step 3: Implement exact style types and fallback**

```csharp
public readonly record struct ScreenshotBackgroundStyle(
    Color Background,
    Color Foreground
);

public static class ScreenshotBackgroundStyleResolver
{
    public static ScreenshotBackgroundStyle Fallback { get; } =
        new(
            Color.FromArgb(235, 17, 24, 39),
            Colors.White
        );
}
```

`Resolve` must:

- return `Fallback` for empty/out-of-bounds bounds;
- support `Bgr24`, `Bgr32`, `Bgra32`, and `Pbgra32`, otherwise return `Fallback`;
- choose 5 evenly spaced X positions and 5 Y positions inside the region, one pixel from edges where dimensions permit;
- sample at most 25 pixels with `CopyPixels`;
- calculate median R/G/B independently;
- calculate luma per sample on `0..255` as `0.2126R + 0.7152G + 0.0722B`;
- classify flat only when luma standard deviation `<= 12` and each RGB range `<= 24`;
- for flat regions return opaque median RGB and black text when normalized median luma `>= 0.55`, otherwise white;
- catch sampling/format exceptions and return `Fallback`.

- [ ] **Step 4: Run resolver GREEN**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests"
```

Expected: PASS.

- [ ] **Step 5: Add RED integration assertions to the window**

Create one flat-light selection and one checkerboard selection. After `ShowResults`, assert the rendered `Border.Background`/`TextBlock.Foreground` colors match the resolver output. These tests must use region bounds that sit fully inside the test bitmap.

- [ ] **Step 6: Run window RED**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationWindowTests"
```

Expected: background assertions fail because the window still uses the old universal dark color.

- [ ] **Step 7: Retain the selection bitmap and apply per-region styles**

Add:

```csharp
private BitmapSource? _selectionImage;
```

Set it in `ApplySelection`:

```csharp
_selectionImage = selection.Image;
```

In `AddTranslationBlock`:

```csharp
var style =
    _selectionImage is null
        ? ScreenshotBackgroundStyleResolver.Fallback
        : ScreenshotBackgroundStyleResolver.Resolve(
            _selectionImage,
            region.Bounds
        );
```

Use `new SolidColorBrush(style.Background)` for the border and `new SolidColorBrush(style.Foreground)` for translated text. Do not alter or persist the source bitmap.

- [ ] **Step 8: Run screenshot-rendering regressions**

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotBackgroundStyleResolverTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests"
```

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add `
  src/LightTranslator/Services/Screenshot/ScreenshotBackgroundStyleResolver.cs `
  src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs `
  tests/LightTranslator.Tests/ScreenshotBackgroundStyleResolverTests.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs

git commit -m "feat: adapt screenshot translation backgrounds"
```

---

### Task 6: Full verification and manual comparison

**Files:**
- No production changes expected.

- [ ] **Step 1: Run full Release tests**

```powershell
dotnet test -c Release
```

Expected: 0 failed, 0 skipped.

- [ ] **Step 2: Run Release build**

```powershell
dotnet build -c Release
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Check dead code, whitespace, and cleanliness**

```powershell
git grep "OcrParagraphGrouper"
git diff --check
git status --short
```

Expected: all three commands produce no output after intended commits.

- [ ] **Step 4: Launch this branch's Release executable**

```powershell
Start-Process "D:\PROJECT2\me\LightTranslator\src\LightTranslator\bin\Release\net8.0-windows\Bridgo.exe"
```

- [ ] **Step 5: Repeat the exact manual comparison scenario**

Verify:

```text
1. Paragraph 1 / Paragraph 2 become separate title regions when geometry supports it.
2. Titles remain visibly larger than body text.
3. Normal body text no longer starts unnecessarily tiny.
4. Long translations still stay inside their OCR rectangles and shrink only as needed.
5. Multi-line paragraphs begin at the top; one-line labels stay centered.
6. Small boxes lose less space to padding.
7. Flat white/light areas blend using sampled light backgrounds and dark text.
8. Complex/game/image areas use the near-opaque dark fallback and white text.
9. Original source text is materially less visible through translated blocks.
10. Click, Esc, and Alt+Q still close the overlay.
```

If any item fails, stop and add the smallest focused RED regression test before changing production code.
