# Phase 2B Structure-Aware Screenshot Translation Design

## Goal

Improve screenshot translation so the rendered result preserves visible text structure and source-scale typography more naturally while keeping the low-latency, no-extra-model constraints already established in Phase 2B.

This design extends the existing fixed-OCR-bounds behavior. It does not replace the previous rule that translated blocks stay inside their original OCR regions and shrink when needed.

## User-Approved Decisions

- Title/body detection uses OCR geometry only. DeepSeek does not classify page structure.
- Font sizing uses OCR geometry plus normalization across similar text in the same screenshot.
- Flat backgrounds reuse a sampled source-region background color; complex backgrounds fall back to a nearly opaque dark translation block.
- Single-line translations are vertically centered; multi-line translations are top aligned.
- Padding is dynamic rather than fixed.
- Title detection is conservative: uncertain cases remain ordinary body text.
- No background-repair or inpainting model is introduced.

## Architecture

Keep the existing high-level pipeline:

`capture -> OCR -> structure analysis -> translation -> layout rendering`

The new structure-analysis step preserves information that is currently lost when raw OCR lines are merged into a paragraph.

### New layout model

Add a screenshot-specific region model separate from the raw OCR model:

```csharp
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

`OcrBlock` remains the raw OCR representation. It must not gain UI/layout-specific properties.

`ScreenshotTextRegion` is produced after OCR filtering and sorting, before translation. It carries merged source text, merged region bounds, representative original single-line height, conservative `Title` or `Body` role, and translated text once translation completes.

`Bounds` and `SourceLineHeight` are stored in the OCR coordinate system: physical screenshot pixels. WPF rendering converts both through the selection DPI before calculating DIP positions or font sizes.

## Structure Analysis

Introduce `ScreenshotTextRegionAnalyzer` as the production structure-analysis component for screenshot translation. It supersedes direct use of `OcrParagraphGrouper` in the screenshot translation pipeline.

`OcrParagraphGrouper` may remain temporarily while tests migrate, but the final production pipeline must have only one grouping path. The implementation plan should remove or retire dead grouping code once the analyzer fully replaces it.

### Body paragraph grouping

Body lines remain grouped using vertical proximity and reading order. The representative `SourceLineHeight` of a merged body region is the median physical-pixel height of its component OCR lines, not the height of the final merged rectangle.

Using the median makes one noisy OCR box less likely to distort the rendered font size.

### Conservative title detection

A line is considered a title only when all of the following are true:

1. It is followed by at least two nearby candidate body lines.
2. The median height of those following body lines can be established.
3. The candidate title line height is at least `1.30x` the following body-line median height.
4. The candidate source text is short: no more than 60 trimmed characters.
5. The title left edge and the first following body-line left edge differ by no more than `max(12 px, 8% of the eventual body-region width)`.
6. The vertical gap from the title bottom to the first body line is no more than `1.5x` the body-line median height.
7. The following candidate body lines have mutually similar heights: each is within `20%` of their median height.

If any condition fails, the line remains `Body`.

These thresholds are deliberately conservative. Missing a title is preferable to misclassifying normal game UI, buttons, chat text, or labels as a title.

### Title and body region boundaries

A detected title becomes its own `ScreenshotTextRegion`. The following body lines are grouped separately. This prevents text such as `Paragraph 1` from being concatenated into the first sentence of the paragraph.

## Translation Behavior

DeepSeek still translates text only. It does not receive or infer `Title`/`Body` roles.

`IScreenshotTextTranslator` remains unchanged. The coordinator adapts each `ScreenshotTextRegion` into a temporary `OcrBlock` with the same `Id`, `Text`, `Confidence`, and `Bounds`, sends those items through the existing batch translation API, then reattaches the returned text to the original regions by `Id`.

A title region and a body region are independent translation items because they are independent visual regions. This may increase the number of text items inside the existing batch compared with the old paragraph-only grouping, but it must not add a second DeepSeek network request. One screenshot translation continues to use the existing single batch request path.

## Font Estimation and Normalization

### Source font estimate

For each region, convert the physical-pixel `SourceLineHeight` to WPF DIP using the selection vertical DPI, then derive the preferred source-scale font from that DIP height:

```text
sourceLineHeightDip = SourceLineHeight * 96 / dpiY
preferredFont = clamp(sourceLineHeightDip * 0.80, 6 DIP, 32 DIP)
```

The upper bound increases from the current 18 DIP so visibly larger headings and larger webpage body text are not artificially reduced before fitting is attempted. DPI conversion must happen before body-font normalization so 100%, 125%, and 150% displays produce equivalent visual sizes.

### Body normalization

Within one screenshot, collect DIP preferred-font estimates for all `Body` regions. Use their median as the representative body font.

A body region whose preferred estimate is within `20%` of the body median uses the median value exactly. A body region outside that tolerance keeps its own estimate. This stabilizes ordinary paragraph text without flattening clearly different UI text sizes.

If no stable body median exists, each body region keeps its own estimate.

### Title sizing

Title regions keep their own preferred estimate and are not normalized down to the body median.

### Fit behavior

For every region:

1. Start at its normalized preferred font size.
2. Measure the translated text inside the usable content area.
3. If it fits, keep that size.
4. If it does not fit, reduce by 1 DIP and re-measure.
5. Stop at 6 DIP.
6. Keep the translation container fixed to the OCR-derived region bounds.
7. Clip at the region boundary if 6 DIP still cannot fit.

This preserves the already-tested Phase 2B fixed-bounds fallback.

## Single-Line vs Multi-Line Alignment

Determine whether the translated text requires wrapping by measuring it at the chosen font size with unconstrained width and comparing the desired width with the usable content width.

- If it fits on one line: `VerticalAlignment.Center`.
- If it requires wrapping: `VerticalAlignment.Top`.

Explicit newline characters also make the region multi-line. The renderer must not ask DeepSeek to insert visual line breaks.

## Dynamic Padding

Padding scales with the mapped OCR region height instead of always using `Thickness(4, 2, 4, 2)`.

```text
horizontal = clamp(regionHeight * 0.12, 1, 4)
vertical   = clamp(regionHeight * 0.06, 0, 2)
```

Apply the same horizontal value on left/right and the same vertical value on top/bottom.

## Background Adaptation

Do not perform inpainting or source-text removal. Inspect the frozen screenshot pixels inside each translated region.

### Sampling

Sample a `5 x 5` evenly distributed grid over the physical-pixel OCR region, staying at least one pixel inside each edge when possible.

For sampled RGB values, calculate:

- median R, G, and B independently
- normalized luma for each sample using `(0.2126 * R + 0.7152 * G + 0.0722 * B) / 255`
- luma standard deviation expressed on the `0..255` scale for the flatness threshold
- per-channel range (`max - min` for R, G, and B)

### Flat-background classification

A region is visually flat only when:

```text
luma standard deviation <= 12
AND
R range <= 24
AND
G range <= 24
AND
B range <= 24
```

If flat:

- use the sampled median RGB as an opaque block background
- compute median normalized luma from the median RGB
- use black text when normalized luma is `>= 0.55`
- otherwise use white text

If complex:

- use fallback background `ARGB(235, 17, 24, 39)`
- use white text

The fallback is intentionally more opaque than the current Phase 2A background so original source text does not strongly show through.

All sampling is local CPU work against the already captured image. No new model, network request, or screenshot persistence is introduced.

## Data Flow

```text
CapturedSelection.Image
        |
        +------------------------------+
        |                              |
        v                              v
OCR raw OcrBlock lines          background sampler
        |
        v
ScreenshotTextRegionAnalyzer
        |
        v
ScreenshotTextRegion[]
        |
        v
adapt to OcrBlock[] for existing batch translator
        |
        v
translation dictionary keyed by Id
        |
        v
translated ScreenshotTextRegion[]
        |
        v
layout renderer
  - normalized source-scale font
  - shrink-to-fit
  - dynamic padding
  - single-line center / multi-line top
  - adaptive flat/complex background
        |
        v
fixed OCR-region overlay
```

## Component Boundaries

### `OcrBlock`

Raw OCR data only. No title/body or rendering metadata is added.

### `ScreenshotTextRegionAnalyzer`

Responsible for reading order, paragraph grouping, conservative title detection, region bounds, representative source line height, and role assignment. It is pure C# and testable without WPF.

### `ScreenshotTextRegion`

Carries structure-analysis output through translation and rendering.

### Screenshot translation coordinator

Orchestrates the same stages as today. It converts filtered OCR output into screenshot regions, adapts those regions to the unchanged translator interface, reattaches translations, and passes translated regions to the result view.

### Result view / renderer

`IScreenshotResultView` changes to the exact interface:

```csharp
void ShowResults(
    IReadOnlyList<ScreenshotTextRegion> regions
);
```

The window continues to receive and retain `CapturedSelection` through its constructor / `ApplySelection` path. `ApplySelection` must retain the `BitmapSource` needed by the background sampler, so `ShowResults` does not receive the selection again.

The renderer is responsible for WPF text measurement, normalized font selection, shrink-to-fit, dynamic padding, vertical alignment, background sampling, and clipping. It does not infer semantic title/body roles.

## Performance Constraints

This work must not add another OCR pass, another DeepSeek network request, image-repair inference, local ONNX layout models, or screenshot disk writes.

Added work is limited to numeric geometry comparisons, median calculations over small collections, WPF text measurement already required for fitting, and at most 25 sampled pixels per rendered text region.

## Failure and Fallback Rules

- If title evidence is insufficient, classify as `Body`.
- If no stable body median exists, use each region's own source-line-height estimate.
- If background pixels cannot be sampled safely, use the complex-background fallback block.
- If text still does not fit at 6 DIP, clip to the fixed region bounds.
- Existing OCR, translation, cancellation, API-key, and network error messages remain unchanged.

## TDD Strategy

Implementation proceeds with focused RED/GREEN cycles.

Required structure-analysis tests:

- separates a larger short heading from two following body lines
- does not classify an isolated large game/UI label as a title
- does not classify a same-size short line as a title
- keeps existing reading order behavior
- groups body lines while preserving median source line height
- splits title and body into independent regions

Required coordinator/translation tests:

- adapts regions to the unchanged batch translator interface
- performs one batch translation call for all regions
- reattaches translations by region `Id`

Required font/layout tests:

- similar body regions normalize to one median preferred font
- an obviously larger title retains a larger preferred font
- 100%, 125%, and 150% DPI convert the same physical/source-scale relationship correctly before normalization
- translated text starts at source-scale font and shrinks only when needed
- single-line translations are vertically centered
- wrapped or explicit-newline translations are top aligned
- dynamic padding is smaller for small OCR regions
- fixed OCR bounds and 6 DIP overflow clipping continue to work

Required background tests:

- flat light background produces sampled light background and dark text
- flat dark background produces sampled dark background and white text
- high-variance background falls back to nearly opaque dark background
- sampling failure falls back safely

After focused tests pass, run the full Release test suite and Release build.

## Acceptance Criteria

This iteration is accepted when manual comparison shows all of the following:

- headings such as `Paragraph 1` are separated from their following body paragraph when geometry supports that conclusion
- ordinary game/chat/UI text is not aggressively misclassified as a heading
- normal body translations render closer to the visible source text scale instead of appearing unnecessarily tiny
- large headings remain visibly larger than body text
- multi-line paragraphs start from the top of their region
- single-line labels remain vertically centered
- small regions lose less space to padding
- original source text is substantially less visible through translated blocks
- flat source backgrounds visually blend better than a universal dark rectangle
- complex backgrounds still have a deterministic readable fallback
- translated regions remain fixed to their OCR bounds
- no new ML model, additional translation request, or background-repair step is added
- full automated tests and Release build remain green
