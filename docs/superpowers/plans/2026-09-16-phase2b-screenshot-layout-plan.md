# Phase 2B Screenshot Translation Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep screenshot translations inside their original OCR paragraph rectangles while using natural wrapping and content-aware font shrinking down to 6 DIP.

**Architecture:** Preserve the existing `capture -> OCR -> paragraph grouping -> translation -> result rendering` pipeline. Localize Phase 2B to `ScreenshotTranslationWindow`: fixed OCR-mapped container geometry, WPF `TextBlock.Measure`-based font fitting, and explicit clipping for the minimum-font overflow case. Do not add models, dependencies, paragraph movement, or background repair.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-16-phase2b-screenshot-layout-design.md`

## Global Constraints

- Keep each translated block at the OCR paragraph's DPI-mapped `X` and `Y` coordinates.
- Keep translated block width and height equal to the mapped OCR paragraph width and height.
- Keep `TextWrapping.Wrap` and `TextTrimming.None`.
- Maximum translation font size remains 18 DIP.
- Minimum adaptive font size is 6 DIP.
- Preserve the existing `Thickness(4, 2, 4, 2)` padding and semi-transparent background.
- Font fitting must measure against the usable content area after padding.
- Do not expand or move translation blocks.
- Do not change OCR, paragraph grouping, translation, cancellation, privacy, or DPI mapping architecture.
- Do not add background cleanup, image inpainting, image-repair models, or new NuGet dependencies.

---

### Task 1: Fix OCR block geometry and add content-aware font fitting

**Files:**
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`
- Test: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`

**Interfaces:**
- Consumes: `OcrBlock.Bounds`, `OcrBlock.TranslatedText`, `DpiCoordinateMapper.PixelsToDips(...)`.
- Produces: fixed-size WPF `Border` translation blocks and a private `CalculateTranslationFontSize(TextBlock text, double sourceHeight, double availableWidth, double availableHeight)` helper.

- [x] **Step 1: Write the failing regression test**

The branch already contains `ShowResults_LongTranslationStaysInsideSourceBoxAndShrinksFont`, which requires the rendered `Border` width and height to equal the mapped OCR bounds, keeps wrapping/no trimming, and requires a fitted font size in the `6..17.999` range.

- [x] **Step 2: Run the focused test and verify RED**

Run:

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests.ShowResults_LongTranslationStaysInsideSourceBoxAndShrinksFont"
```

Observed RED on 2026-09-16:

```text
Expected: 24
Actual:   NaN
```

This proves the current production code still leaves `Border.Height` unset and allows the translation block to grow beyond the source OCR rectangle.

- [ ] **Step 3: Implement the minimum fixed-layout and font-fitting behavior**

In `ScreenshotTranslationWindow.xaml.cs`, change the minimum font constant and use one shared padding value:

```csharp
private const double MinimumTranslationFontSize = 6d;
private const double MaximumTranslationFontSize = 18d;

private static readonly Thickness TranslationPadding =
    new(
        4,
        2,
        4,
        2
    );
```

In `AddTranslationBlock`, construct the `TextBlock` without assigning a fixed font first, calculate the usable content area, then fit the font and create a fixed-size container:

```csharp
var text =
    new TextBlock
    {
        Text =
            block.TranslatedText,
        Foreground =
            System.Windows.Media.Brushes.White,
        TextWrapping =
            TextWrapping.Wrap,
        TextTrimming =
            TextTrimming.None,
        VerticalAlignment =
            VerticalAlignment.Center
    };

var availableWidth =
    Math.Max(
        0d,
        bounds.Width -
        TranslationPadding.Left -
        TranslationPadding.Right
    );

var availableHeight =
    Math.Max(
        0d,
        bounds.Height -
        TranslationPadding.Top -
        TranslationPadding.Bottom
    );

text.FontSize =
    CalculateTranslationFontSize(
        text,
        bounds.Height,
        availableWidth,
        availableHeight
    );

var container =
    new Border
    {
        Width =
            bounds.Width,
        Height =
            bounds.Height,
        Padding =
            TranslationPadding,
        Background =
            new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    199,
                    17,
                    24,
                    39
                )
            ),
        CornerRadius =
            new CornerRadius(
                3
            ),
        Child =
            text
    };
```

Replace the old height-only `CalculateTranslationFontSize(double sourceHeight)` helper with content-aware measurement:

```csharp
private static double CalculateTranslationFontSize(
    TextBlock text,
    double sourceHeight,
    double availableWidth,
    double availableHeight
)
{
    var preferred =
        Math.Clamp(
            sourceHeight * 0.80d,
            MinimumTranslationFontSize,
            MaximumTranslationFontSize
        );

    if (availableWidth <= 0d ||
        availableHeight <= 0d)
    {
        return MinimumTranslationFontSize;
    }

    var candidate =
        preferred;

    while (candidate > MinimumTranslationFontSize)
    {
        text.FontSize =
            candidate;

        text.Measure(
            new Size(
                availableWidth,
                double.PositiveInfinity
            )
        );

        if (text.DesiredSize.Height <= availableHeight)
        {
            return candidate;
        }

        candidate =
            Math.Max(
                MinimumTranslationFontSize,
                candidate - 1d
            );
    }

    return MinimumTranslationFontSize;
}
```

This intentionally keeps the 6 DIP minimum even when an unusually tiny OCR rectangle is shorter than 6 DIP; Task 2 handles the corresponding visual overflow by clipping to the fixed OCR bounds.

Do not add clipping in this task; Task 2 defines that behavior with its own RED test first.

- [ ] **Step 4: Update the existing window geometry assertion to the new intentional behavior**

In `ScreenshotTranslationWindowTests.ShowResults_RendersOnlyTranslatedTextAtMappedBounds`, replace the old auto-height assertions:

```csharp
Assert.True(
    double.IsNaN(
        translatedBlock.Height
    )
);

Assert.Equal(
    32,
    translatedBlock.MinHeight,
    6
);
```

with the fixed-height assertion:

```csharp
Assert.Equal(
    32,
    translatedBlock.Height,
    6
);
```

Keep the existing coordinate, width, translated-text-only, and `TextTrimming.None` assertions unchanged.

- [ ] **Step 5: Run the focused layout and window tests**

Run:

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests|FullyQualifiedName~ScreenshotTranslationWindowTests"
```

Expected: all selected tests PASS.

- [ ] **Step 6: Commit Task 1**

```powershell
git add `
  src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs

git commit -m "feat: fit screenshot translations inside OCR bounds"
```

---

### Task 2: Constrain the extreme minimum-font overflow case

**Files:**
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`

**Interfaces:**
- Consumes: the fixed-size `Border` and 6 DIP minimum font behavior from Task 1.
- Produces: explicit visual clipping at the OCR rectangle when text still cannot fit at 6 DIP.

- [ ] **Step 1: Write a failing test for an impossible-to-fit translation**

Add this test to `ScreenshotTranslationLayoutRegressionTests`:

```csharp
[Fact]
public void ShowResults_ExtremeTranslationStopsAtMinimumFontAndClipsToSourceBox()
{
    RunOnSta(
        () =>
        {
            var window =
                new ScreenshotTranslationWindow(
                    CreateSelection()
                );

            try
            {
                window.ShowResults(
                    new[]
                    {
                        new OcrBlock(
                            "block-0001",
                            "X",
                            0.95,
                            new PixelRect(
                                20,
                                20,
                                60,
                                12
                            ),
                            new string(
                                '译',
                                200
                            )
                        )
                    }
                );

                var canvas =
                    Assert.IsType<Canvas>(
                        window.FindName(
                            "TranslationCanvas"
                        )
                    );

                var container =
                    Assert.Single(
                        canvas.Children
                            .OfType<Border>()
                    );

                var translatedText =
                    Assert.IsType<TextBlock>(
                        container.Child
                    );

                Assert.Equal(
                    6d,
                    translatedText.FontSize,
                    6
                );

                Assert.True(
                    container.ClipToBounds
                );

                Assert.Equal(
                    48d,
                    container.Width,
                    6
                );

                Assert.Equal(
                    9.6d,
                    container.Height,
                    6
                );
            }
            finally
            {
                window.Close();
            }
        }
    );
}
```

- [ ] **Step 2: Run only the new test and verify RED**

Run:

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests.ShowResults_ExtremeTranslationStopsAtMinimumFontAndClipsToSourceBox"
```

Expected: FAIL because `Border.ClipToBounds` is still `false`.

- [ ] **Step 3: Add the minimum production change**

In the `Border` initializer inside `AddTranslationBlock`, add:

```csharp
ClipToBounds =
    true,
```

Do not change layout geometry, OCR behavior, or translation behavior.

- [ ] **Step 4: Run the complete screenshot-layout/window test slice**

Run:

```powershell
dotnet test `
  --filter "FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests|FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~DpiCoordinateMapperTests"
```

Expected: all selected tests PASS.

- [ ] **Step 5: Commit Task 2**

```powershell
git add `
  src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs `
  tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs

git commit -m "fix: constrain screenshot translation overflow"
```

---

### Task 3: Full regression and release-build verification

**Files:**
- No production file changes expected.
- Verify the complete repository state produced by Tasks 1 and 2.

**Interfaces:**
- Consumes: all Phase 2B layout changes.
- Produces: verification evidence that Phase 2B does not regress Phase 2A or the rest of Bridgo.

- [ ] **Step 1: Run the full Release test suite**

```powershell
dotnet test -c Release
```

Expected: 0 failed tests and 0 skipped tests unless an already-documented repository condition says otherwise.

- [ ] **Step 2: Build Release configuration**

```powershell
dotnet build -c Release
```

Expected: build succeeds with 0 warnings and 0 errors.

- [ ] **Step 3: Check whitespace and repository cleanliness**

```powershell
git diff --check
git status --short
```

Expected: `git diff --check` prints nothing. `git status --short` prints nothing after all intended changes are committed.

- [ ] **Step 4: Manual smoke check the layout behavior**

Run Bridgo from the branch and check at least:

```text
1. A short translated paragraph keeps a normal readable font size.
2. A long translated paragraph wraps and shrinks instead of extending below its OCR block.
3. Multiple translated blocks stay at their original OCR coordinates and do not move each other.
4. Screenshot result close behavior still works by click, Esc, and Alt+Q.
5. Test at Windows display scaling 100%, 125%, or 150% available on the current machine; automated DPI mapping tests cover the remaining mapped-coordinate cases.
```

If a manual smoke item fails, stop and reproduce it with a focused failing test before changing production code.
