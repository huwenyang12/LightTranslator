# Phase 3 Apple-Inspired UI Polish Design

Date: 2026-09-18  
Baseline: `main` at `28d3dd4`  
Working branch: `feature/phase3-ui-polish`

## 1. Goal

Phase 3 turns the current functional WPF interface into a cohesive, release-quality desktop experience without adding product features or changing the translation, OCR, screenshot, hotkey, tray, settings, or privacy behavior.

The visual direction is Apple-inspired restraint adapted to Windows: clear hierarchy, generous spacing, semantic color, subtle depth, restrained motion, and native Windows material behavior. It must not imitate macOS window chrome or introduce decorative glass throughout the content layer.

## 2. Scope

The phase covers:

- shared theme resources and reusable control styles;
- the text translation window;
- the first-run and settings window;
- screenshot capture visuals;
- screenshot translation loading, error, and result presentation;
- light and dark appearance, high-contrast-safe fallbacks, keyboard focus, DPI behavior, and motion restraint;
- UI-focused regression tests and final Release verification.

The phase does not cover:

- new translation providers, languages, history, accounts, cloud sync, or export;
- OCR, translation, screenshot coordination, persistence, or privacy pipeline changes;
- migration from WPF to WinUI;
- a new main application dashboard;
- macOS traffic-light controls or direct copying of Apple platform chrome.

## 3. Design Principles

1. **Calm hierarchy:** content is primary; controls remain quiet until needed.
2. **Consistent tokens:** pages consume semantic resources instead of hard-coded colors and measurements.
3. **Material with purpose:** the long-lived settings window uses a Mica-compatible base on supported Windows 11 systems; the transient translation window uses an Acrylic-compatible treatment on supported systems. Both use the defined solid theme surface whenever the system effect is unavailable or disabled.
4. **Native Windows behavior:** keyboard navigation, window behavior, DPI scaling, system theme, and accessibility remain familiar on Windows.
5. **No functional regression:** existing commands, bindings, control names used by tests, and automation-visible behavior remain intact unless a UI test explicitly defines the replacement.
6. **Low distraction:** animation is limited to short opacity or position transitions and respects reduced-motion expectations.

## 4. Visual Foundation

### 4.1 Typography

- Primary family: `Segoe UI Variable`, with `Segoe UI` fallback.
- Display/title: 24 DIP, Semibold.
- Section title: 15-16 DIP, Semibold.
- Body/input: 14-16 DIP, Regular.
- Caption/status: 11-12 DIP, Regular or Medium.
- Avoid thin weights that reduce Chinese legibility.

### 4.2 Spacing and shape

- Spacing scale: 4, 8, 12, 16, 24, 32 DIP.
- Small control radius: 8 DIP.
- Button radius: 10 DIP.
- Card radius: 14 DIP.
- Floating window radius: 18 DIP.
- Borders remain one physical-pixel-equivalent where practical.

### 4.3 Semantic color

Light defaults:

- window base: `#F5F5F7`;
- elevated surface: near-white with restrained translucency;
- primary text: `#1D1D1F`;
- secondary text: `#6E6E73`;
- separator: `#D2D2D7`;
- accent: `#007AFF`;
- destructive/error: `#D70015`.

Dark defaults:

- window base: `#1C1C1E`;
- elevated surface: `#2C2C2E`;
- primary text: `#F5F5F7`;
- secondary text: `#AEAEB2`;
- separator: `#3A3A3C`;
- accent: `#0A84FF`;
- destructive/error: `#FF453A`.

All XAML uses semantic brush keys so colors can switch by theme and fall back to solid values when transparency is disabled.

### 4.4 Shared controls

Create reusable styles for:

- primary, secondary, quiet icon, and circular icon buttons;
- text boxes and password boxes;
- combo boxes;
- check boxes;
- section cards;
- validation and status text;
- keyboard focus visuals;
- small shortcut/key-cap hints.

Existing automation names and event handlers are preserved.

## 5. Window Designs

### 5.1 Text translation window

The translation window becomes the visual reference for the product.

- Use a compact floating card with a 560 x 360 DIP default size, an 18 DIP outer radius, and subtle elevation.
- Use a restrained translucent treatment with an opaque solid fallback.
- Present source and target language selectors in a quiet top command row.
- Replace the text swap glyph treatment with a consistent icon-style circular button while preserving its command.
- Separate input and output primarily through spacing and a subtle divider, not multiple nested cards.
- Keep source input immediately focusable.
- Present copy as a quiet icon/button aligned with the translation result.
- Keep current Enter, Shift+Enter, and Esc behavior.
- Present errors in a compact semantic status surface rather than an isolated red text line.
- Keep keyboard help visible but secondary.

### 5.2 First-run and settings window

Use the same window for both modes while improving hierarchy.

- Header contains app identity, current mode title, and supporting copy.
- Content is grouped into Account/API, Text Translation, Screenshot Translation, and Startup/System cards.
- Field labels, values, validation, and actions align consistently.
- Connection testing shows neutral, busy, success, and error states without modal interruption.
- The save action is visually primary only when changes are valid and pending.
- The current set of options does not justify sidebar navigation.

### 5.3 Screenshot capture

- Preserve the frozen-screen and drag-to-select interaction.
- Use a consistent dim layer and accent selection border.
- Present selection dimensions in a small floating pill.
- Show a short, low-priority instruction hint; Esc remains the cancel action.
- Do not add a capture toolbar or editing tools.

### 5.4 Screenshot translation result

- Preserve adaptive sampled backgrounds and the complex-background fallback.
- Replace the current central status block with a compact status pill and restrained progress treatment.
- Use short cross-fades between preparing, OCR, translating, error, and result states.
- Keep translation block geometry, clipping, title/body sizing, and close interactions unchanged.
- Do not apply global glass effects to translated text regions because legibility and source occlusion take priority.

### 5.5 Tray surfaces

Keep the native tray menu behavior, wording, item order, and shortcuts unchanged in Phase 3. The existing application icon remains the tray identity.

## 6. Architecture

### 6.1 Resource organization

`App.xaml` merges focused resource dictionaries:

- `Resources/Theme/Colors.xaml`
- `Resources/Theme/Typography.xaml`
- `Resources/Theme/Metrics.xaml`
- `Resources/Styles/Buttons.xaml`
- `Resources/Styles/Inputs.xaml`
- `Resources/Styles/Cards.xaml`

Resource keys describe meaning rather than appearance, for example `Brush.Text.Primary`, `Brush.Surface.Card`, and `Style.Button.Primary`.

### 6.2 Theme behavior

Theme resources follow the Windows app theme where technically reliable in WPF. UI code must have deterministic light, dark, transparency-disabled, and high-contrast fallbacks. Theme handling remains presentation-only and does not enter view models or business services.

### 6.3 Material behavior

Backdrop support is isolated behind a small window-presentation helper so unsupported Windows versions and disabled transparency fall back safely. The helper must not become a dependency of translation, settings, or screenshot services.

### 6.4 View boundaries

Existing view models, services, and controller contracts remain unchanged unless a presentation-only property is demonstrably required. UI logic stays in view resources or view code-behind; business logic is not moved into XAML event handlers.

## 7. Accessibility and Interaction

- Maintain visible keyboard focus on all actionable controls.
- Preserve tab order and screen-reader-friendly labels.
- Validate contrast for normal, secondary, disabled, error, and focused states.
- Support Windows high contrast with system-color fallbacks.
- Avoid depending on transparency or color alone to communicate state.
- Keep minimum practical pointer targets near 32-36 DIP for compact desktop use.
- Verify Chinese, English, and Japanese labels do not clip at 100%, 125%, and 150% scaling.

## 8. Testing Strategy

Implementation proceeds in small UI-focused test cycles.

Automated coverage should verify:

- required resource dictionaries are merged and expected semantic keys exist;
- windows consume shared styles instead of reintroducing critical hard-coded colors;
- existing control names, bindings, commands, and keyboard behaviors remain intact;
- settings validation and persistence behavior remain unchanged;
- screenshot capture and translation layout regression tests remain green;
- theme/material helpers use safe fallbacks.

Manual verification should cover:

- light and dark appearance;
- transparency enabled and disabled;
- Windows high contrast;
- 100%, 125%, and 150% scaling;
- keyboard-only operation;
- first-run mode and subsequent settings mode;
- text translation normal, loading, success, and error states;
- screenshot capture, OCR/loading, translation result, cancellation, and failure states;
- Windows 10 fallback and Windows 11 material behavior where environments are available.

## 9. Implementation Order

1. **Phase 3A — Visual foundation:** semantic tokens, shared styles, theme/backdrop fallback, and regression guards.
2. **Phase 3B — Core windows:** text translation window first, then first-run/settings.
3. **Phase 3C — Screenshot experience:** capture visuals, status presentation, and restrained transitions.
4. **Phase 3D — Quality and release:** accessibility, theme, DPI, keyboard, full tests, Release build, manual checklist, and release version decision.

## 10. Acceptance Criteria

Phase 3 is complete when:

- all user-facing windows share one recognizable visual language;
- the translation window feels fast, focused, and visually polished;
- settings are grouped, readable, and consistent in first-run and edit modes;
- screenshot capture and result states are polished without slowing the workflow;
- no product feature or translation/OCR behavior has changed;
- solid fallbacks remain usable without transparency;
- keyboard, accessibility, DPI, and multilingual labels are verified;
- the complete Release test suite passes and Release build reports zero warnings and errors;
- the branch is ready for a dedicated `v0.3.0` or `v1.0.0-rc` release decision.
