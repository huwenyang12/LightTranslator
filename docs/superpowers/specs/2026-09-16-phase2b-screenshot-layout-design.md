# Phase 2B Screenshot Translation Layout Design

## Goal

Improve screenshot translation readability without adding background repair or extra inference cost. Translated text must remain inside the original OCR paragraph bounds and use natural wrapping plus adaptive font sizing.

## Scope

Phase 2B is limited to screenshot translation layout behavior.

Included:
- keep each translated block at the OCR paragraph's mapped position
- keep translated block width equal to the OCR paragraph width
- keep translated block height equal to the OCR paragraph height
- keep `TextWrapping.Wrap`
- keep `TextTrimming.None`
- calculate font size from the available content box and translated text length
- shrink the font when wrapped text does not fit the original OCR bounds
- allow font size to shrink to 6 DIP for extreme cases
- preserve the existing semi-transparent translated block background
- preserve existing DPI mapping behavior

Excluded:
- background cleanup or image inpainting
- local image-repair models
- expanding translation blocks outside OCR bounds
- moving or avoiding neighboring translation blocks
- cross-monitor screenshot selection
- continuous/game-chat monitoring
- history, editing, copy, or export features

## Layout Rule

For every translated `OcrBlock`:

1. Convert `OcrBlock.Bounds` from physical pixels to WPF DIPs using the existing `DpiCoordinateMapper`.
2. Create a translation container at exactly the mapped `X` and `Y` coordinates.
3. Set the container `Width` and `Height` to exactly the mapped OCR width and height.
4. Preserve the current padding and semi-transparent background.
5. Start from the preferred translation font size derived from the source box height, capped by the current maximum font size.
6. Measure the translated text using the usable content width after padding and natural wrapping.
7. If the measured wrapped text height exceeds the usable content height, reduce font size and re-measure.
8. Stop once the text fits or the 6 DIP minimum font size is reached.
9. Never enlarge or move the OCR translation container to accommodate longer translated text.

The fixed OCR bounds are the primary layout constraint. Readability is improved by wrapping and shrinking, not by changing block geometry.

## Font Sizing

- maximum translation font size remains 18 DIP
- preferred starting size remains based on source box height
- minimum adaptive font size becomes 6 DIP
- sizing is content-aware rather than based only on source height
- the fitting calculation must account for the container padding so measured text does not assume the full outer border dimensions

If a translation is still too long at 6 DIP, the layout remains constrained to the original OCR box. Phase 2B does not introduce expansion or overflow-management architecture.

## Existing Architecture

The screenshot pipeline remains unchanged:

`capture -> OCR -> paragraph grouping -> translation -> result rendering`

`OcrParagraphGrouper`, translation services, screenshot coordination, privacy behavior, cancellation, and OCR models are not changed by this phase.

The production change should stay localized to screenshot-result layout/rendering, primarily `ScreenshotTranslationWindow`.

## Testing Strategy

Use TDD.

The first regression test must prove that a long translation:
- renders in a container whose width equals the mapped OCR width
- renders in a container whose height equals the mapped OCR height
- keeps natural wrapping
- does not use ellipsis trimming
- receives a font size below the previous fixed-size result when necessary
- never goes below 6 DIP

Existing screenshot window, DPI mapping, OCR, translation, coordinator, and full-suite tests must remain green.

## Acceptance Criteria

Phase 2B layout work is accepted when:
- long translations no longer grow the translated block beyond the original OCR height
- long translations shrink their font to fit the original OCR rectangle where possible
- short translations continue to render normally
- translated blocks remain at their mapped OCR coordinates
- 100%, 125%, and 150% DPI coordinate behavior is unchanged
- no background repair dependency or model is added
- focused layout tests pass
- the complete test suite passes
- Release build completes without warnings or errors
