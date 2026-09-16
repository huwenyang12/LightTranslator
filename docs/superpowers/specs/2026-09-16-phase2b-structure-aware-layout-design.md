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

`ScreenshotTextRegion` is produced after OCR filtering and sorting, before translation. It carries:

- merged source text
- merged region bounds
- representative original single-line height
- conservative `Title` or `Body` role
- translated text once translation completes

This keeps OCR responsibilities separate from screenshot layout responsibilities.

## Structure Analysis

Replace the current paragraph-only grouping result with structure-aware regions while retaining the existing reading-order behavior.

### Body paragraph grouping

Body lines remain grouped using vertical proximity and reading order. The representative `SourceLineHeight` of a merged body region is the median height of its component OCR lines, not the height of the final merged rectangle.

Using the median makes one noisy OCR box less likely to distort the rendered font size.

### Conservative title detection

A line is considered a title only when all of the following are true:

1. It is followed by at least two nearby candidate body lines.
2. The median height of those following body lines can be established.
3. The candidate title line height is at least `1.30x` the following body-line median height.
4. The candidate source text is short: no more than 60 trimmed characters.
5. The candidate and following body block are left-aligned within `max(12 px, 8% of the body region width)`.
6. The vertical gap from the title bottom to the first body line is no more than `1.5x` the body-line median height.
7. The following body lines have mutually similar heights: each is within `20%` of their median height.

If any condition fails, the line remains `Body`.

These thresholds are deliberately conservative. Missing a title is preferable to misclassifying normal game UI, buttons, chat text, or labels as a title.

### Title and body region boundaries

A detected title becomes its own `ScreenshotTextRegion`.

The following body lines are grouped separately. This prevents text such as `Paragraph 1` from being concatenated into the first sentence of the paragraph.

## Translation Behavior

DeepSeek still translates text only. It does not receive or infer layout roles.

The screenshot translation service may continue to accept a list of text-bearing items as long as title/body metadata remains local and is reattached after translation. The translation count must not increase merely because structure metadata exists.

A title region and a body region are independent translation units because they are independent visual regions.

No extra translation request is added beyond the existing batch request; all regions remain part of the same batch where supported by the existing translator.

## Font Estimation and Normalization

### Source font estimate

For each region, derive the preferred source-scale font from `SourceLineHeight` rather than merged region height.

The initial target font is:

```text
preferredFont = clamp(SourceLineHeight * 0.80, 6 DIP, 32 DIP)
```

The upper bound increases from the current 18 DIP so visibly larger headings and larger webpage body text are not artificially reduced before fitting is attempted.

### Body normalization

Within one screenshot, collect preferred font estimates for all `Body` regions.

Use the median body preferred font as the representative body font. A body region whose preferred estimate is within `20%` of the body median uses the median value exactly.

A body region outside that tolerance keeps its own estimate. This avoids forcing clearly different UI text sizes into one global size while stabilizing ordinary paragraph text.

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

Determine whether the translated text requires wrapping by measuring it once at the chosen font size with unconstrained width and comparing the desired width to the available content width.

- If it fits on one line: `VerticalAlignment.Center`.
- If it requires wrapping: `VerticalAlignment.Top`.

The renderer must not rely on semantic newlines from DeepSeek to decide visual line layout.

## Dynamic Padding

Padding must scale with the OCR region instead of always using `Thickness(4, 2, 4, 2)`.

Use this deterministic policy in DIP coordinates:

```text
horizontal = clamp(regionHeight * 0.12, 1, 4)
vertical   = clamp(regionHeight * 0.06, 0, 2)
```

Apply the same horizontal value on left/right and the same vertical value on top/bottom.

This keeps small UI labels from losing a large percentage of usable height while preserving comfortable spacing in larger regions.

## Background Adaptation

Do not perform inpainting or source-text removal.

Instead, inspect the frozen screenshot pixels inside each translated region.

### Sampling

Sample a `5 x 5` evenly distributed grid over the region, staying at least one pixel inside each edge when possible.

For sampled RGB values, calculate:

- median RGB color
- luminance for each sample
- luminance standard deviation
- per-channel range (`max - min` for R, G, and B)

### Flat-background classification

A region is considered visually flat only when:

```text
luminance standard deviation <= 12
AND
R range <= 24
AND
G range <= 24
AND
B range <= 24
```

If flat:

- use the sampled median RGB as an opaque block background
- choose black or white text according to relative luminance
- use black text for relative luminance >= 0.55
- otherwise use white text

If complex:

- use fallback background `ARGB(235, 17, 24, 39)`
- use white text

The fallback is intentionally more opaque than the current Phase 2A background so the original source text does not strongly show through.

All sampling is local CPU work against the already captured image. No new model, network request, or screenshot persistence is introduced.

## Data Flow

The revised flow is:

```text
CapturedSelection.Image
        |
        +------------------------------+
        |                              |
        v                              v
OCR raw OcrBlock lines          background sampler
        |
        v
structure analyzer
        |
        v
ScreenshotTextRegion[]
        |
        v
batch text translation
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

Remains raw OCR data only. No title/body or rendering metadata is added.

### Structure analyzer

Responsible for:

- reading order
- paragraph grouping
- conservative title detection
- region bounds
- representative source line height
- role assignment

It must be testable without WPF.

### `ScreenshotTextRegion`

Carries structure-analysis output through translation and rendering.

### Screenshot translation coordinator

Orchestrates the same stages as today. It converts filtered OCR output into screenshot regions, translates their text in the existing batch flow, reattaches translations, and passes regions plus the frozen selection to rendering as needed.

### Renderer/window

Responsible only for visual layout and background sampling. It does not infer semantic title/body roles.

## Interface Impact

`IScreenshotResultView.ShowResults(...)` must no longer depend on plain `OcrBlock` if the renderer needs role and source-line-height metadata.

The preferred interface is conceptually:

```csharp
void ShowResults(
    CapturedSelection selection,
    IReadOnlyList<ScreenshotTextRegion> regions
);
```

If the existing view already retains the same `CapturedSelection` instance reliably, the implementation may keep selection state internal and pass only regions. The final implementation plan must choose one explicit form and use it consistently.

## Performance Constraints

This work must not add:

- another OCR pass
- another DeepSeek call
- image-repair inference
- local ONNX layout models
- screenshot disk writes

The added work is limited to:

- numeric geometry comparisons
- median calculations over small collections
- WPF text measurement already required for fitting
- at most 25 sampled pixels per rendered text region

These operations should remain small relative to OCR and network translation latency.

## Failure and Fallback Rules

- If title evidence is insufficient, classify as `Body`.
- If no stable body median exists, use each region's own source-line-height estimate.
- If background pixels cannot be sampled safely, use the complex-background fallback block.
- If text still does not fit at 6 DIP, clip to the fixed region bounds.
- Existing OCR, translation, cancellation, API-key, and network error messages remain unchanged.

## TDD Strategy

Implementation must proceed with focused RED/GREEN cycles.

Required structure-analysis tests:

- separates a larger short heading from two following body lines
- does not classify an isolated large game/UI label as a title
- does not classify a same-size short line as a title
- keeps existing reading order behavior
- groups body lines while preserving median source line height
- splits title and body into independent regions

Required font/layout tests:

- similar body regions normalize to one median preferred font
- an obviously larger title retains a larger preferred font
- translated text starts at source-scale font and shrinks only when needed
- single-line translations are vertically centered
- wrapped translations are top aligned
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
- no new ML model, translation request, or background-repair step is added
- full automated tests and Release build remain green
