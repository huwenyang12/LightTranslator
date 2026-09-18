# Phase 3 Apple-Inspired UI Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a cohesive Apple-inspired, Windows-native visual system for Bridgo without changing translation, OCR, screenshot, settings, tray, privacy, or persistence behavior.

**Architecture:** Centralize semantic brushes, typography, measurements, and reusable control styles in merged WPF resource dictionaries. Keep system appearance and DWM backdrop integration behind small presentation-only Windows services, then migrate each existing window onto the shared resources while preserving its bindings, names, commands, and event handlers.

**Tech Stack:** C# 12, .NET 8, WPF XAML, xUnit, Win32 DWM interop already compatible with the Windows-only target.

**Spec:** `docs/superpowers/specs/2026-09-18-phase3-apple-inspired-ui-design.md`

## Global Constraints

- Remain on `net8.0-windows`, WPF, and the existing single application project.
- Add no product feature, language, provider, history, account, cloud, export, or editing capability.
- Do not modify OCR, translation, screenshot coordination, persistence, privacy, tray behavior, or hotkey behavior.
- Preserve every existing automation-visible control name, binding, command, and keyboard interaction.
- Use `Segoe UI Variable` with `Segoe UI` fallback; do not bundle SF Pro or Apple design assets.
- Use semantic resource keys; do not add new hard-coded production colors outside palette dictionaries and screenshot source-adaptive rendering.
- Use solid fallbacks whenever transparency, DWM backdrop, or theme detection is unavailable.
- Keep the native tray menu behavior, wording, item order, shortcuts, and application icon unchanged.
- Verify Windows light, dark, high contrast, transparency fallback, 100%/125%/150% DPI, keyboard navigation, Chinese/English/Japanese labels, all Release tests, and a warning-free Release build.

---

## File Map

New presentation resources:

- `src/LightTranslator/Resources/Theme/Palette.Light.xaml` — light semantic brushes.
- `src/LightTranslator/Resources/Theme/Palette.Dark.xaml` — dark semantic brushes.
- `src/LightTranslator/Resources/Theme/Palette.HighContrast.xaml` — system-color accessibility brushes.
- `src/LightTranslator/Resources/Theme/Typography.xaml` — font families, sizes, and text styles.
- `src/LightTranslator/Resources/Theme/Metrics.xaml` — spacing, radii, and standard control measurements.
- `src/LightTranslator/Resources/Styles/Buttons.xaml` — primary, secondary, quiet, and circular button styles.
- `src/LightTranslator/Resources/Styles/Inputs.xaml` — text, password, combo, and checkbox styles.
- `src/LightTranslator/Resources/Styles/Cards.xaml` — surfaces, status pills, key caps, and section headers.

New presentation services:

- `src/LightTranslator/Services/Windows/SystemAppearance.cs` — `Light`, `Dark`, and `HighContrast` enum.
- `src/LightTranslator/Services/Windows/SystemAppearanceResolver.cs` — pure appearance selection policy.
- `src/LightTranslator/Services/Windows/ThemeManager.cs` — swaps only the palette dictionary and listens for system preference changes.
- `src/LightTranslator/Services/Windows/WindowBackdropService.cs` — applies Mica/Acrylic-compatible DWM attributes when supported and otherwise leaves the solid XAML fallback.

New tests:

- `tests/LightTranslator.Tests/UiResourceTests.cs` — merged dictionaries and semantic-key contract.
- `tests/LightTranslator.Tests/SystemAppearanceResolverTests.cs` — theme policy.
- `tests/LightTranslator.Tests/WindowBackdropServiceTests.cs` — backdrop support policy and safe fallback.

Existing files modified in later tasks:

- `src/LightTranslator/App.xaml` and `App.xaml.cs` — load resources and initialize presentation services.
- `src/LightTranslator/Views/TranslateWindow.xaml(.cs)` — floating translator redesign and transient backdrop.
- `src/LightTranslator/Views/FirstRunSettingsWindow.xaml(.cs)` — grouped settings cards and long-lived backdrop.
- `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml(.cs)` — visual selection treatment, hint, and dimension pill.
- `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml(.cs)` — status pill, progress treatment, and shared tokens.
- Existing view tests — preserve behavior while adding UI contract assertions.

---

### Task 1: Semantic theme resources

**Files:**
- Create: `src/LightTranslator/Resources/Theme/Palette.Light.xaml`
- Create: `src/LightTranslator/Resources/Theme/Palette.Dark.xaml`
- Create: `src/LightTranslator/Resources/Theme/Palette.HighContrast.xaml`
- Create: `src/LightTranslator/Resources/Theme/Typography.xaml`
- Create: `src/LightTranslator/Resources/Theme/Metrics.xaml`
- Modify: `src/LightTranslator/App.xaml`
- Create: `tests/LightTranslator.Tests/UiResourceTests.cs`

**Interfaces:**
- Produces palette keys `Brush.Window.Base`, `Brush.Surface.Card`, `Brush.Surface.Elevated`, `Brush.Text.Primary`, `Brush.Text.Secondary`, `Brush.Text.OnAccent`, `Brush.Border.Subtle`, `Brush.Accent`, `Brush.Accent.Hover`, `Brush.Error`, `Brush.Success`, `Brush.Focus`.
- Produces typography styles `Style.Text.Display`, `Style.Text.Section`, `Style.Text.Body`, `Style.Text.Caption`.
- Produces metrics `Space.4`, `Space.8`, `Space.12`, `Space.16`, `Space.24`, `Space.32`, `Radius.8`, `Radius.10`, `Radius.14`, `Radius.18`, `Height.Control`, `Height.Button`.

- [ ] **Step 1: Write the resource contract test**

Add an STA test that constructs `App`, flattens merged dictionaries, and asserts every interface key above exists. Also parse all three palette XAML files with `XDocument.Load` and assert each palette exposes the same set of `x:Key` values.

```csharp
[Fact]
public void AppResources_ExposeRequiredSemanticUiKeys()
{
    RunOnSta(() =>
    {
        var app = new App();
        var required = new[]
        {
            "Brush.Window.Base", "Brush.Surface.Card", "Brush.Surface.Elevated",
            "Brush.Text.Primary", "Brush.Text.Secondary", "Brush.Text.OnAccent",
            "Brush.Border.Subtle", "Brush.Accent", "Brush.Accent.Hover",
            "Brush.Error", "Brush.Success", "Brush.Focus",
            "Style.Text.Display", "Style.Text.Section", "Style.Text.Body",
            "Style.Text.Caption", "Space.4", "Space.8", "Space.12", "Space.16",
            "Space.24", "Space.32", "Radius.8", "Radius.10", "Radius.14",
            "Radius.18", "Height.Control", "Height.Button"
        };

        foreach (var key in required)
        {
            Assert.NotNull(app.Resources[key]);
        }
    });
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~UiResourceTests`

Expected: FAIL because semantic resources do not exist.

- [ ] **Step 3: Add the palette, typography, and metric dictionaries**

Use `SolidColorBrush` resources for all semantic palette values. Use light values `#F5F5F7`, `#FFFFFF`, `#1D1D1F`, `#6E6E73`, `#D2D2D7`, `#007AFF`, `#0066D6`, `#D70015`, `#248A3D`; dark values `#1C1C1E`, `#2C2C2E`, `#F5F5F7`, `#AEAEB2`, `#3A3A3C`, `#0A84FF`, `#409CFF`, `#FF453A`, `#30D158`; and system brushes such as `{x:Static SystemColors.WindowBrush}` and `{x:Static SystemColors.WindowTextBrush}` in high contrast.

Merge Light, Typography, and Metrics in `App.xaml` in that order. Give the palette dictionary the exact source URI `/Bridgo;component/Resources/Theme/Palette.Light.xaml` so `ThemeManager` can identify and replace it later.

- [ ] **Step 4: Run focused and full tests**

Run: `dotnet test -c Release --filter FullyQualifiedName~UiResourceTests`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: all existing tests plus the new tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/LightTranslator/App.xaml src/LightTranslator/Resources/Theme tests/LightTranslator.Tests/UiResourceTests.cs
git commit -m "feat: add semantic UI theme resources"
```

### Task 2: Shared control and surface styles

**Files:**
- Create: `src/LightTranslator/Resources/Styles/Buttons.xaml`
- Create: `src/LightTranslator/Resources/Styles/Inputs.xaml`
- Create: `src/LightTranslator/Resources/Styles/Cards.xaml`
- Modify: `src/LightTranslator/App.xaml`
- Modify: `tests/LightTranslator.Tests/UiResourceTests.cs`

**Interfaces:**
- Consumes all Task 1 brush, typography, and metric keys.
- Produces `Style.Button.Primary`, `Style.Button.Secondary`, `Style.Button.Quiet`, `Style.Button.IconCircle`, `Style.Input.TextBox`, `Style.Input.PasswordBox`, `Style.Input.ComboBox`, `Style.Input.CheckBox`, `Style.Surface.Card`, `Style.Surface.Status`, `Style.Surface.KeyCap`, and `Style.Text.Validation`.

- [ ] **Step 1: Extend the failing resource contract**

Add the produced style keys to `AppResources_ExposeRequiredSemanticUiKeys`. Add a test that loads each style dictionary as XML and fails if it contains a six/eight-digit hard-coded hex color; palette files and the existing source-adaptive screenshot renderer are the only allowed color-definition locations.

```csharp
[Theory]
[InlineData("Buttons.xaml")]
[InlineData("Inputs.xaml")]
[InlineData("Cards.xaml")]
public void SharedStyleDictionary_UsesSemanticBrushes(string fileName)
{
    var text = File.ReadAllText(GetStylePath(fileName));
    Assert.DoesNotMatch("#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?", text);
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~UiResourceTests`

Expected: FAIL because shared style files and keys do not exist.

- [ ] **Step 3: Implement reusable styles**

Each style must define a `ControlTemplate` with a rounded `Border`, semantic focus visual, disabled opacity of `0.45`, and no platform-default chrome leakage. Primary uses `Brush.Accent`/`Brush.Text.OnAccent`; secondary uses `Brush.Surface.Elevated`/`Brush.Border.Subtle`; quiet uses a transparent background and semantic foreground; icon-circle is 32 x 32 DIP. Inputs are 36 DIP high, use 10 DIP radius, and show `Brush.Focus` on keyboard focus. Cards use 14 DIP radius and 1 DIP subtle border. Status and key-cap surfaces use 8 DIP radius.

Merge Buttons, Inputs, and Cards after the Task 1 dictionaries in `App.xaml`.

- [ ] **Step 4: Run focused and full tests**

Run: `dotnet test -c Release --filter FullyQualifiedName~UiResourceTests`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/LightTranslator/App.xaml src/LightTranslator/Resources/Styles tests/LightTranslator.Tests/UiResourceTests.cs
git commit -m "feat: add shared WPF control styles"
```

### Task 3: System appearance and backdrop fallbacks

**Files:**
- Create: `src/LightTranslator/Services/Windows/SystemAppearance.cs`
- Create: `src/LightTranslator/Services/Windows/SystemAppearanceResolver.cs`
- Create: `src/LightTranslator/Services/Windows/ThemeManager.cs`
- Create: `src/LightTranslator/Services/Windows/WindowBackdropService.cs`
- Modify: `src/LightTranslator/App.xaml.cs`
- Create: `tests/LightTranslator.Tests/SystemAppearanceResolverTests.cs`
- Create: `tests/LightTranslator.Tests/WindowBackdropServiceTests.cs`

**Interfaces:**
- Produces `SystemAppearanceResolver.Resolve(bool highContrast, int appsUseLightTheme) : SystemAppearance`.
- Produces `ThemeManager.Start(Application application)`, `ThemeManager.Stop()`, and `ThemeManager.Apply(Application application, SystemAppearance appearance)`.
- Produces `WindowBackdropService.TryApply(Window window, WindowBackdropKind kind) : bool` and enum values `Mica`, `TransientAcrylic`.
- `TryApply` returns `false` without throwing on unsupported Windows versions, unavailable DWM composition, disabled effects, or native-call failure.

- [ ] **Step 1: Write appearance policy tests**

```csharp
[Theory]
[InlineData(true, 1, SystemAppearance.HighContrast)]
[InlineData(false, 1, SystemAppearance.Light)]
[InlineData(false, 0, SystemAppearance.Dark)]
public void Resolve_ReturnsExpectedAppearance(
    bool highContrast,
    int appsUseLightTheme,
    SystemAppearance expected)
{
    Assert.Equal(expected, SystemAppearanceResolver.Resolve(highContrast, appsUseLightTheme));
}
```

Add backdrop policy tests for `IsSupported(Version)` using Windows 11 build `22621` as supported and Windows 10 build `19045` as unsupported.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test -c Release --filter "FullyQualifiedName~SystemAppearanceResolverTests|FullyQualifiedName~WindowBackdropServiceTests"`

Expected: build FAIL because the types do not exist.

- [ ] **Step 3: Implement appearance resolution and palette swapping**

`ThemeManager` reads `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`, gives `SystemParameters.HighContrast` precedence, swaps only the palette dictionary URI, and subscribes to `SystemEvents.UserPreferenceChanged`. `Stop()` unsubscribes. Registry access failures select Light. Dispatch palette replacement through `Application.Dispatcher`.

- [ ] **Step 4: Implement the safe DWM adapter**

Use `DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE = 38` and `DWMWA_WINDOW_CORNER_PREFERENCE = 33`. Map Mica to `DWMSBT_MAINWINDOW = 2`, transient Acrylic to `DWMSBT_TRANSIENTWINDOW = 3`, and rounded corners to `DWMWCP_ROUND = 2`. Apply after `SourceInitialized`; catch `DllNotFoundException`, `EntryPointNotFoundException`, and native failure codes. Never remove the XAML solid background fallback.

- [ ] **Step 5: Start and stop theme observation with application lifetime**

Create one `ThemeManager` in `App.OnStartup`, call `Start(this)` before user windows are created, and call `Stop()` in `OnExit`. Do not inject it into controllers or view models.

- [ ] **Step 6: Run focused and full tests**

Run: `dotnet test -c Release --filter "FullyQualifiedName~SystemAppearanceResolverTests|FullyQualifiedName~WindowBackdropServiceTests"`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/LightTranslator/App.xaml.cs src/LightTranslator/Services/Windows tests/LightTranslator.Tests/SystemAppearanceResolverTests.cs tests/LightTranslator.Tests/WindowBackdropServiceTests.cs
git commit -m "feat: follow system appearance with safe backdrops"
```

### Task 4: Text translation floating window

**Files:**
- Modify: `src/LightTranslator/Views/TranslateWindow.xaml`
- Modify: `src/LightTranslator/Views/TranslateWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/TranslateWindowLanguagePersistenceTests.cs`
- Modify: `tests/LightTranslator.Tests/TranslateViewModelTests.cs` only if a pre-existing behavior assertion needs a clearer name; do not change behavior.
- Create: `tests/LightTranslator.Tests/TranslateWindowVisualTests.cs`

**Interfaces:**
- Consumes shared styles and `WindowBackdropService.TryApply(this, WindowBackdropKind.TransientAcrylic)`.
- Preserves `SourceTextBox`, source/target language bindings, `OnSwapLanguagesClick`, `OnCopyTranslationClick`, Enter, Shift+Enter, Esc, focus, and closing behavior.
- Produces named presentation elements `TranslationSurface`, `CopyTranslationButton`, `ErrorStatusBorder`, and `KeyboardHintTextBlock` for UI contract tests.

- [ ] **Step 1: Write the visual contract test**

Construct the window on an STA thread and assert default `Width == 560`, `Height == 360`, `TranslationSurface.CornerRadius == new CornerRadius(18)`, the named copy/error/hint elements exist, `SourceTextBox` still exists, and its binding path remains `SourceText`.

```csharp
Assert.Equal(560d, window.Width);
Assert.Equal(360d, window.Height);
Assert.Equal(new CornerRadius(18), surface.CornerRadius);
Assert.Equal("SourceText", BindingOperations.GetBinding(sourceBox, TextBox.TextProperty)?.Path.Path);
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~TranslateWindowVisualTests`

Expected: FAIL on size and missing named elements.

- [ ] **Step 3: Rebuild the XAML using shared resources**

Use a transparent outer grid with 24 DIP shadow breathing room and `TranslationSurface` as the 18 DIP card. Set a compact top command row, 16 DIP body text, semantic divider, quiet copy button, semantic error surface, and secondary key hint `Enter 复制 · Shift+Enter 换行 · Esc 关闭`. Replace every existing hard-coded UI color with a dynamic semantic brush. Keep combo item codes and bindings unchanged.

- [ ] **Step 4: Apply transient backdrop after source initialization**

Call `WindowBackdropService.TryApply` from the window constructor through `SourceInitialized`. The window stays usable if it returns false.

- [ ] **Step 5: Run translation-window and full tests**

Run: `dotnet test -c Release --filter "FullyQualifiedName~TranslateWindow|FullyQualifiedName~TranslateViewModel"`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/LightTranslator/Views/TranslateWindow.xaml src/LightTranslator/Views/TranslateWindow.xaml.cs tests/LightTranslator.Tests/TranslateWindowVisualTests.cs tests/LightTranslator.Tests/TranslateWindowLanguagePersistenceTests.cs
git commit -m "feat: redesign translation floating window"
```

### Task 5: First-run and settings cards

**Files:**
- Modify: `src/LightTranslator/Views/FirstRunSettingsWindow.xaml`
- Modify: `src/LightTranslator/Views/FirstRunSettingsWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/FirstRunSettingsWindowTests.cs`
- Modify: `tests/LightTranslator.Tests/ProductBrandingTests.cs`

**Interfaces:**
- Consumes shared styles and `WindowBackdropService.TryApply(this, WindowBackdropKind.Mica)`.
- Preserves all current named input, error, language, swap, test, checkbox, and save controls plus their handlers.
- Produces named cards `AccountSettingsCard`, `TextTranslationSettingsCard`, `ScreenshotTranslationSettingsCard`, and `StartupSettingsCard`.

- [ ] **Step 1: Write failing card and mode tests**

Add an STA test that constructs first-run and normal windows and asserts the four named cards exist, `TitleTextBlock.Text` is `首次设置` in first-run mode and `设置` in normal mode, and existing API/hotkey/language/save controls remain discoverable by name.

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~FirstRunSettingsWindowTests`

Expected: FAIL because card names do not exist.

- [ ] **Step 3: Recompose the settings XAML**

Use a 560 x 720 DIP default window with a Mica-compatible semantic base and scrollable content. Add a header with the existing app icon, `语桥 Bridgo`, mode title, and concise support text. Place existing controls into the four cards without changing bindings or event handlers. Use a sticky bottom action row with the existing `SaveButton`; preserve its enablement binding/updates. Use shared validation style on all error text.

- [ ] **Step 4: Apply Mica and retain solid fallback**

Apply the backdrop on `SourceInitialized`. Keep `Brush.Window.Base` behind the entire content tree so Windows 10, high contrast, remote desktop, and disabled effects stay legible.

- [ ] **Step 5: Run settings and full tests**

Run: `dotnet test -c Release --filter "FullyQualifiedName~FirstRunSettings|FullyQualifiedName~ProductBranding"`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/LightTranslator/Views/FirstRunSettingsWindow.xaml src/LightTranslator/Views/FirstRunSettingsWindow.xaml.cs tests/LightTranslator.Tests/FirstRunSettingsWindowTests.cs tests/LightTranslator.Tests/ProductBrandingTests.cs
git commit -m "feat: redesign first-run and settings window"
```

### Task 6: Screenshot capture visual feedback

**Files:**
- Modify: `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml`
- Modify: `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotCaptureWindowTests.cs`

**Interfaces:**
- Preserves frozen-screen capture, coordinate conversion, mouse capture, `Selection`, `DialogResult`, and Esc cancellation.
- Produces named `CaptureInstructionPill`, `SelectionDimensionPill`, and `SelectionDimensionTextBlock`.
- Produces internal pure formatter `FormatSelectionDimensions(PixelRect selection) : string`, returning strings such as `640 × 360`.

- [ ] **Step 1: Write failing dimension and surface tests**

```csharp
[Fact]
public void FormatSelectionDimensions_UsesPixelDimensions()
{
    Assert.Equal(
        "640 × 360",
        ScreenshotCaptureWindow.FormatSelectionDimensions(new PixelRect(10, 20, 640, 360))
    );
}
```

Add an STA test that verifies the two pill elements and dimension text exist and that `SelectionRectangle.Stroke` resolves to the semantic accent brush.

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~ScreenshotCaptureWindowTests`

Expected: build FAIL because the formatter and named elements do not exist.

- [ ] **Step 3: Implement capture visuals and dimension updates**

Use a consistent `#66000000` dim layer, 2 DIP accent selection stroke, 8 DIP selection radius, a top-center instruction pill reading `拖动选择翻译区域 · Esc 取消`, and a dimension pill positioned 8 DIP below the active selection when space permits or 8 DIP above it otherwise. Convert the current DIP bounds through `DpiCoordinateMapper.DipsToPixels` before formatting so the displayed values match captured pixels. Hide the dimension pill before dragging and after empty selection cancellation.

- [ ] **Step 4: Run capture and full tests**

Run: `dotnet test -c Release --filter FullyQualifiedName~ScreenshotCaptureWindowTests`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/LightTranslator/Views/ScreenshotCaptureWindow.xaml src/LightTranslator/Views/ScreenshotCaptureWindow.xaml.cs tests/LightTranslator.Tests/ScreenshotCaptureWindowTests.cs
git commit -m "feat: polish screenshot capture feedback"
```

### Task 7: Screenshot translation status presentation

**Files:**
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`

**Interfaces:**
- Preserves `ShowLoading`, both `ShowResults` overloads, `ShowMessage`, translation block geometry, clipping, adaptive backgrounds, and close bindings.
- Produces named `StatusProgressBar` and retains `StatusBorder` and `StatusTextBlock`.
- `ShowStatus(string, bool)` controls progress visibility; loading uses `true`, terminal message uses `false`, results hide the full status surface.

- [ ] **Step 1: Write failing loading/message/result state tests**

Add STA assertions:

```csharp
window.ShowLoading(selection, "正在识别…");
Assert.Equal(Visibility.Visible, progress.Visibility);

window.ShowMessage("未识别到可翻译文字");
Assert.Equal(Visibility.Collapsed, progress.Visibility);

window.ShowResults(Array.Empty<ScreenshotTextRegion>());
Assert.Equal(Visibility.Collapsed, statusBorder.Visibility);
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test -c Release --filter FullyQualifiedName~ScreenshotTranslationWindowTests`

Expected: FAIL because `StatusProgressBar` and state-specific visibility do not exist.

- [ ] **Step 3: Redesign the status pill**

Use shared status surface, semantic foreground, 14 DIP radius, 16/12 DIP padding, a 28 DIP indeterminate progress bar styled as a quiet accent line or compact ring substitute, and the existing message text. Use a 120ms opacity transition only when the window is loaded; tests and non-rendered windows must update final visibility synchronously. Replace the hard-coded window/status colors with semantic resources. Do not change generated translation-block background colors from `ScreenshotBackgroundStyleResolver`.

- [ ] **Step 4: Run screenshot-layout and full tests**

Run: `dotnet test -c Release --filter "FullyQualifiedName~ScreenshotTranslationWindowTests|FullyQualifiedName~ScreenshotTranslationLayoutRegressionTests|FullyQualifiedName~ScreenshotBackgroundStyleResolverTests"`

Expected: PASS.

Run: `dotnet test -c Release`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/LightTranslator/Views/ScreenshotTranslationWindow.xaml src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs
git commit -m "feat: polish screenshot translation status"
```

### Task 8: Accessibility, static UI audit, and release verification

**Files:**
- Modify: `tests/LightTranslator.Tests/UiResourceTests.cs`
- Modify: any Phase 3 XAML file only when this task exposes a concrete audit failure.
- Create: `docs/phase3-ui-acceptance.md`

**Interfaces:**
- Consumes all Phase 3 presentation resources and windows.
- Produces a repeatable static audit and manual acceptance record.

- [ ] **Step 1: Add failing static audit tests**

Scan the four user-facing XAML files and assert they contain no six/eight-digit hard-coded colors except the screenshot capture dim mask `#66000000`. Assert all named interactive elements have either visible text, `ToolTip`, or `AutomationProperties.Name`. Assert `KeyboardNavigation.TabNavigation` is not disabled on settings or translation content roots.

- [ ] **Step 2: Run the audit and confirm any RED findings**

Run: `dotnet test -c Release --filter FullyQualifiedName~UiResourceTests`

Expected before cleanup: FAIL on remaining hard-coded colors or unlabeled icon controls.

- [ ] **Step 3: Fix only reported presentation defects**

Replace remaining colors with semantic brushes, add concise Chinese automation names to icon-only controls, correct focus order, and retain the single allowed capture dim mask. Do not refactor unrelated code.

- [ ] **Step 4: Run complete automated verification**

Run: `dotnet test -c Release`

Expected: all tests PASS, zero failures, zero skips unless an existing platform-specific skip is already present.

Run: `dotnet build -c Release`

Expected: build succeeds with 0 warnings and 0 errors.

Run: `git diff --check`

Expected: no output.

- [ ] **Step 5: Perform manual UI acceptance**

Record pass/fail and screenshots or observations in `docs/phase3-ui-acceptance.md` for:

1. light theme;
2. dark theme;
3. Windows high contrast;
4. transparency disabled;
5. 100%, 125%, and 150% display scaling;
6. keyboard-only translation and settings;
7. Chinese, English, and Japanese labels without clipping;
8. first-run and normal settings modes;
9. translation loading, success, error, copy, Enter, Shift+Enter, and Esc;
10. screenshot select, dimension pill, cancel, loading, success, no-text, error, click close, Esc, and Alt+Q close;
11. Windows 10 solid fallback and Windows 11 backdrop behavior where those environments are available.

- [ ] **Step 6: Commit verification artifacts**

```powershell
git add src/LightTranslator tests/LightTranslator.Tests docs/phase3-ui-acceptance.md
git commit -m "test: complete Phase 3 UI acceptance"
```

### Task 9: Final branch review and release decision

**Files:**
- Modify: `src/LightTranslator/LightTranslator.csproj` only after the user chooses `v0.3.0` or `v1.0.0-rc`; do not guess.
- Modify: `tests/LightTranslator.Tests/ProductBrandingTests.cs` only alongside an approved version change.

**Interfaces:**
- Produces a reviewed, clean branch suitable for merge.

- [ ] **Step 1: Review the complete branch diff**

Run: `git diff --stat main...HEAD`

Run: `git diff --check main...HEAD`

Run: `git status --short --branch`

Expected: intentional Phase 3 files only, no whitespace errors, clean worktree.

- [ ] **Step 2: Re-run release evidence immediately before completion**

Run: `dotnet test -c Release`

Expected: all tests PASS.

Run: `dotnet build -c Release`

Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Ask the user for the release version decision**

Present `v0.3.0` as the recommended continuation because this phase changes presentation without declaring the original V1 product contract final. Offer `v1.0.0-rc.1` only if the user wants to begin release-candidate stabilization.

- [ ] **Step 4: Apply and test the approved version, or leave `0.2.0` unchanged**

If `v0.3.0` is chosen, update `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` to `0.3.0`/`0.3.0.0`, rename the branding test to `ApplicationAssembly_UsesV030ReleaseVersion`, and assert those exact values. If `v1.0.0-rc.1` is chosen, use assembly/file version `1.0.0.0` and informational version `1.0.0-rc.1` with matching assertions.

- [ ] **Step 5: Commit the approved version change when applicable**

```powershell
git add src/LightTranslator/LightTranslator.csproj tests/LightTranslator.Tests/ProductBrandingTests.cs
git commit -m "chore: prepare approved UI release version"
```

- [ ] **Step 6: Hand off the clean branch**

Report the branch, commit range, test total, Release build result, manual acceptance exceptions, and recommended merge/publish command. Do not merge, tag, push, or publish without explicit user authorization.
