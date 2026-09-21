# Screenshot Translation Typesetting Round 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refine screenshot translation typography, column alignment, paragraph spacing, and bottom safety while preserving source coverage.

**Architecture:** Keep typography decisions in `ScreenshotTranslationTypography`, add a small pure layout-policy service for alignment and spacing, and have `ScreenshotTranslationWindow` consume both policies while retaining the existing WPF overlay structure.

**Tech Stack:** .NET 8, C#, WPF, xUnit

**Spec:** `docs/superpowers/specs/2026-09-21-screenshot-translation-typesetting-round2-design.md`

## Global Constraints

- Do not change OCR or translation behavior.
- Preserve opaque coverage of each original OCR region.
- Keep translated text readable and non-overlapping.
- Use test-first development for every behavior change.

## Review Focus

- A single oversized OCR line must not dominate ordinary body text.
- Small auxiliary body text must remain subordinate but readable.
- Numbered paragraph labels must remain visibly distinct from body text.
- Nearby columns should align, while genuinely separate columns remain independent.
- A final region near the bottom must cover its source without placing translated text beyond the window.

---

### Task 1: Normalize the typography hierarchy

**Files:**
- Modify: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationTypography.cs`
- Test: `tests/LightTranslator.Tests/ScreenshotTranslationTypographyTests.cs`

**Interfaces:**
- Consumes: `ScreenshotTextRegion`, DPI Y.
- Produces: `CalculatePreferredFontSizes(IReadOnlyList<ScreenshotTextRegion>, double)` with median-based body and title sizing.

- [ ] **Step 1: Write failing tests** for oversized body clamping, small-body flooring, compact numbered labels, and modest ordinary-title emphasis.
- [ ] **Step 2: Run the typography tests** and verify failures are caused by the old sizing policy.
- [ ] **Step 3: Implement the minimal median-based hierarchy** using a 0.63 physical scale and explicit role ratios.
- [ ] **Step 4: Run the typography tests** and verify they pass.
- [ ] **Step 5: Commit** the typography behavior and tests.

### Task 2: Apply column alignment and spacing policy

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationLayout.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Test: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutTests.cs`
- Test: `tests/LightTranslator.Tests/ScreenshotTranslationLayoutRegressionTests.cs`
- Test: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`

**Interfaces:**
- Consumes: ordered screenshot regions, mapped bounds, roles, and preferred font sizes.
- Produces: `CalculateAlignedLeftEdges(...)` and `CalculateParagraphGap(...)` layout decisions used by the WPF view.

- [ ] **Step 1: Write failing layout-policy tests** for nearby-column alignment, independent distant columns, and role-aware gaps.
- [ ] **Step 2: Run the layout tests** and verify the missing policy fails.
- [ ] **Step 3: Implement the pure layout policy** with a 24-DIP column tolerance and bounded role-aware gaps.
- [ ] **Step 4: Write failing WPF regression tests** for aligned text insets, tighter line height, and a 20-DIP bottom safe area.
- [ ] **Step 5: Update the window** to consume the policy without shrinking source coverage.
- [ ] **Step 6: Run focused tests and the complete suite** and verify all pass.
- [ ] **Step 7: Build Release with zero warnings and commit** the layout behavior and tests.

