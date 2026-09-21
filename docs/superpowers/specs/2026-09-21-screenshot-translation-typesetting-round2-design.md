# Screenshot Translation Typesetting Round 2 Design

## Goal

Bring the screenshot translation overlay closer to the WeChat reference by refining typography and spacing without changing OCR or translation behavior.

## Confirmed scope

- Normalize body text around the page median so unusually large or small OCR lines do not dominate the page.
- Keep ordinary headings slightly above body size, while compact numbered paragraph labels such as `第1段` remain smaller and clearly separated.
- Align nearby text blocks to a shared column edge without moving the source-covering background away from the original OCR bounds.
- Use role-aware paragraph gaps and reserve a bottom safe area for translated text.
- Preserve the first-round fixes for opaque source coverage, readable minimum text, no overlap, and complex-background fallback.

## Non-goals

- No OCR changes.
- No translation prompt or wording changes.
- No new controls or interaction behavior.

## Design

Typography is calculated from physical OCR line height, then normalized against the median body size. Body text is clamped to a restrained range around that median. Numbered section labels receive a compact fixed ratio; other titles retain modest emphasis. The overlay view uses a slightly tighter line-height ratio.

A small layout policy groups nearby left edges into columns and returns a shared alignment anchor. The source-covering border remains large enough to hide the original OCR region; only the translated text inset is aligned. Paragraph clearance is derived from adjacent roles and font size. The last region receives a bottom safety limit whenever the source bounds leave room for it.

