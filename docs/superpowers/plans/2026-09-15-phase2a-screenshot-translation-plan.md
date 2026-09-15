# LightTranslator Phase 2A Screenshot Translation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a production-ready Windows screenshot translation flow that captures one monitor region, performs bundled PP-OCRv5 Mobile OCR locally, sends only numbered OCR text to DeepSeek, and renders translated text at the original block positions.

**Architecture:** `ScreenshotTranslationCoordinator` owns one cancellable workflow and delegates monitor capture, selection, OCR, batch translation, and result rendering through focused interfaces. Physical pixels remain the canonical coordinate system; one pure mapper converts pixels to WPF device-independent units at the UI boundary. A single bundled PP-OCRv5 Mobile universal detector/recognizer serves `auto`, `zh`, `en`, and `ja` without runtime downloads.

**Tech Stack:** .NET 8, WPF, xUnit, System.Drawing screen capture, Microsoft.ML.OnnxRuntime 1.30.0 CPU package, PP-OCRv5 Mobile universal models, DeepSeek Chat Completions API.

**Spec:** `docs/superpowers/specs/2026-09-15-phase2a-screenshot-translation-design.md`

## Global Constraints

- Work on `feature/phase2a-screenshot-translation`; do not move or rewrite `main` or tag `v0.1.0`.
- Target `net8.0-windows` and Windows x64; the published application must be self-contained and must not require Python or a separately installed OCR runtime.
- Bundle one PP-OCRv5 Mobile universal detection model, one universal recognition model, and their character dictionary; runtime model downloads and model writes under `%LOCALAPPDATA%` are forbidden.
- The same OCR model serves `auto`, `zh`, `en`, and `ja`; language settings affect translation semantics only.
- Screenshots and OCR images stay in memory; only OCR text plus source/target language is sent to DeepSeek.
- Never log screenshots, source text, translated text, coordinates, local paths, API keys, authorization headers, or raw response bodies.
- One screenshot workflow may be active. A second `Alt+Q` or tray request cancels and closes the active workflow instead of starting another.
- Phase 2A is current-monitor-only and supports 100%, 125%, and 150% DPI.
- OCR blocks below confidence `0.50` are excluded; retained blocks are ordered top-to-bottom, then left-to-right.
- The result overlay stays topmost and closes on one click, `Esc`, or a second `Alt+Q`.
- Preserve all Phase 1 behavior and keep the complete Release test suite green.

---

## File Structure

### New production files

- `src/LightTranslator/Models/PixelRect.cs` — immutable physical-pixel rectangle.
- `src/LightTranslator/Models/ScreenCaptureFrame.cs` — frozen monitor bitmap plus monitor pixel bounds and DPI.
- `src/LightTranslator/Models/CapturedSelection.cs` — frozen selected bitmap plus absolute and relative pixel bounds and DPI.
- `src/LightTranslator/Models/OcrBlock.cs` — stable OCR block ID, text, confidence, pixel bounds, and optional translation.
- `src/LightTranslator/Services/ScreenCapture/IDisplayCaptureService.cs` — monitor capture/crop contract.
- `src/LightTranslator/Services/ScreenCapture/DisplayCaptureService.cs` — cursor-monitor selection and memory-only screen capture.
- `src/LightTranslator/Services/ScreenCapture/DpiCoordinateMapper.cs` — pure pixel/DIP conversion.
- `src/LightTranslator/Services/Ocr/IOcrService.cs` — asynchronous OCR contract.
- `src/LightTranslator/Services/Ocr/OcrModelPaths.cs` — validated model asset paths.
- `src/LightTranslator/Services/Ocr/OcrModelException.cs` — model initialization failure category.
- `src/LightTranslator/Services/Ocr/IOcrModelProvider.cs` — bundled model lookup contract.
- `src/LightTranslator/Services/Ocr/OcrModelProvider.cs` — executable-relative asset validation.
- `src/LightTranslator/Services/Ocr/OcrImagePreprocessor.cs` — detector and recognizer tensor preparation.
- `src/LightTranslator/Services/Ocr/DbDetectorPostProcessor.cs` — detector probability map to axis-aligned text boxes.
- `src/LightTranslator/Services/Ocr/CtcTextDecoder.cs` — recognition logits to text/confidence.
- `src/LightTranslator/Services/Ocr/PaddleOcrService.cs` — ONNX detector/recognizer orchestration.
- `src/LightTranslator/Services/Translation/IScreenshotTextTranslator.cs` — numbered OCR batch translation contract.
- `src/LightTranslator/Services/Translation/DeepSeekScreenshotTextTranslator.cs` — privacy-safe DeepSeek batch request and ID mapping.
- `src/LightTranslator/Services/Screenshot/IScreenshotCaptureView.cs` — selection UI abstraction.
- `src/LightTranslator/Services/Screenshot/IScreenshotResultView.cs` — loading/result/error overlay abstraction.
- `src/LightTranslator/Services/Screenshot/IScreenshotResultViewFactory.cs` — result window factory.
- `src/LightTranslator/Services/Screenshot/ScreenshotTranslationCoordinator.cs` — single-flight state machine and cancellation owner.
- `src/LightTranslator/Services/Settings/IScreenshotLanguageSettingsPersistence.cs` — screenshot language persistence contract.
- `src/LightTranslator/Services/Settings/ScreenshotLanguageSettingsPersistence.cs` — settings update implementation.
- `src/LightTranslator/Assets/Ocr/ppocrv5_mobile_det.onnx` — bundled official ONNX detector.
- `src/LightTranslator/Assets/Ocr/ppocrv5_mobile_rec.onnx` — bundled official ONNX universal recognizer.
- `src/LightTranslator/Assets/Ocr/ppocrv5_dict.txt` — matching recognition dictionary.
- `src/LightTranslator/Assets/Ocr/THIRD-PARTY-NOTICES.md` — model source, version, checksum, and license notice.
- `tools/prepare_ocr_models.py` — development-only reproducible model acquisition and verification script.
- `tools/test_prepare_ocr_models.py` — model acquisition and checksum regression tests.

### Modified production files

- `src/LightTranslator/LightTranslator.csproj` — ONNX Runtime dependency and model publish rules.
- `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml` — frozen monitor background and dim selection layer.
- `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml.cs` — monitor-bound selection returning physical-pixel coordinates.
- `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml` — frozen crop, loading/error state, and overlay canvas.
- `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs` — implement result view and close gestures.
- `src/LightTranslator/ViewModels/FirstRunSettingsViewModel.cs` — screenshot source/target language properties and save flow.
- `src/LightTranslator/Views/FirstRunSettingsWindow.xaml` — screenshot language selectors.
- `src/LightTranslator/Views/FirstRunSettingsWindow.xaml.cs` — compose screenshot language persistence.
- `src/LightTranslator/Controllers/IScreenshotTranslationView.cs` — document toggle semantics while retaining the Phase 1 controller boundary.
- `src/LightTranslator/App.xaml.cs` — compose and dispose Phase 2A services.
- `docs/phase2a-acceptance.md` — manual acceptance record and privacy/publish checks.

### New or modified tests

- `tests/LightTranslator.Tests/DpiCoordinateMapperTests.cs`
- `tests/LightTranslator.Tests/DisplayCaptureServiceTests.cs`
- `tests/LightTranslator.Tests/OcrBlockTests.cs`
- `tests/LightTranslator.Tests/OcrModelProviderTests.cs`
- `tests/LightTranslator.Tests/OcrImagePreprocessorTests.cs`
- `tests/LightTranslator.Tests/DbDetectorPostProcessorTests.cs`
- `tests/LightTranslator.Tests/CtcTextDecoderTests.cs`
- `tests/LightTranslator.Tests/PaddleOcrServiceTests.cs`
- `tests/LightTranslator.Tests/DeepSeekScreenshotTextTranslatorTests.cs`
- `tests/LightTranslator.Tests/ScreenshotTranslationCoordinatorTests.cs`
- `tests/LightTranslator.Tests/ScreenshotLanguageSettingsPersistenceTests.cs`
- `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`
- `tests/LightTranslator.Tests/FirstRunSettingsViewModelTests.cs`
- `tests/LightTranslator.Tests/FirstRunSettingsWindowTests.cs`
- `tests/LightTranslator.Tests/AppLoggerTests.cs`
- `tests/LightTranslator.Tests/AppControllerTests.cs`

---

### Task 1: Physical-pixel domain model and DPI mapper

**Files:**
- Create: `src/LightTranslator/Models/PixelRect.cs`
- Create: `src/LightTranslator/Models/ScreenCaptureFrame.cs`
- Create: `src/LightTranslator/Models/CapturedSelection.cs`
- Create: `src/LightTranslator/Models/OcrBlock.cs`
- Create: `src/LightTranslator/Services/ScreenCapture/DpiCoordinateMapper.cs`
- Create: `tests/LightTranslator.Tests/DpiCoordinateMapperTests.cs`
- Create: `tests/LightTranslator.Tests/OcrBlockTests.cs`

**Interfaces:**
- Consumes: WPF device-independent units and per-monitor DPI values.
- Produces: `PixelRect`, `ScreenCaptureFrame`, `CapturedSelection`, `OcrBlock`, `DpiCoordinateMapper.PixelsToDips(PixelRect, double, double)`, and `DpiCoordinateMapper.DipsToPixels(Rect, double, double)`.

- [ ] **Step 1: Add failing coordinate and OCR-block tests**

```csharp
[Theory]
[InlineData(96, 0, 0, 1000, 600, 0, 0, 1000, 600)]
[InlineData(120, 100, 50, 1000, 600, 80, 40, 800, 480)]
[InlineData(144, 150, 75, 900, 450, 100, 50, 600, 300)]
public void PixelsToDips_UsesMonitorDpi(
    double dpi, int x, int y, int width, int height,
    double expectedX, double expectedY, double expectedWidth, double expectedHeight)
{
    var actual = DpiCoordinateMapper.PixelsToDips(
        new PixelRect(x, y, width, height), dpi, dpi);

    Assert.Equal(expectedX, actual.X, 6);
    Assert.Equal(expectedY, actual.Y, 6);
    Assert.Equal(expectedWidth, actual.Width, 6);
    Assert.Equal(expectedHeight, actual.Height, 6);
}

[Fact]
public void FilterAndSort_RemovesLowConfidenceAndUsesReadingOrder()
{
    var blocks = new[]
    {
        new OcrBlock("b", "right", 0.90, new PixelRect(200, 10, 80, 20)),
        new OcrBlock("low", "secret", 0.49, new PixelRect(0, 0, 10, 10)),
        new OcrBlock("a", "left", 0.80, new PixelRect(10, 12, 80, 20)),
        new OcrBlock("c", "next", 0.70, new PixelRect(10, 80, 80, 20))
    };

    var actual = OcrBlock.FilterAndSort(blocks, 0.50);

    Assert.Equal(new[] { "a", "b", "c" }, actual.Select(x => x.Id));
}
```

- [ ] **Step 2: Run the focused tests and confirm the missing-type failure**

Run: `dotnet test --filter "DpiCoordinateMapperTests|OcrBlockTests"`

Expected: build fails because `PixelRect`, `DpiCoordinateMapper`, and `OcrBlock` do not exist.

- [ ] **Step 3: Add the minimal immutable types and pure conversions**

```csharp
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public sealed record OcrBlock(
    string Id,
    string Text,
    double Confidence,
    PixelRect Bounds,
    string? TranslatedText = null)
{
    public static IReadOnlyList<OcrBlock> FilterAndSort(
        IEnumerable<OcrBlock> blocks,
        double minimumConfidence) => blocks
            .Where(x => x.Confidence >= minimumConfidence && !string.IsNullOrWhiteSpace(x.Text))
            .OrderBy(x => x.Bounds.Y / 12)
            .ThenBy(x => x.Bounds.X)
            .ToArray();
}

public static class DpiCoordinateMapper
{
    public static Rect PixelsToDips(PixelRect value, double dpiX, double dpiY) =>
        new(value.X * 96d / dpiX, value.Y * 96d / dpiY,
            value.Width * 96d / dpiX, value.Height * 96d / dpiY);

    public static PixelRect DipsToPixels(Rect value, double dpiX, double dpiY) =>
        new((int)Math.Round(value.X * dpiX / 96d),
            (int)Math.Round(value.Y * dpiY / 96d),
            (int)Math.Round(value.Width * dpiX / 96d),
            (int)Math.Round(value.Height * dpiY / 96d));
}
```

`ScreenCaptureFrame` stores a frozen `BitmapSource Image`, `PixelRect MonitorBounds`, `double DpiX`, and `double DpiY`. `CapturedSelection` stores a frozen `BitmapSource Image`, `PixelRect MonitorRelativeBounds`, `PixelRect ScreenBounds`, `double DpiX`, and `double DpiY`. Constructors reject empty bounds and non-positive DPI.

- [ ] **Step 4: Run focused and regression tests**

Run: `dotnet test --filter "DpiCoordinateMapperTests|OcrBlockTests"`

Expected: PASS, including exact 96/120/144 DPI conversions and confidence filtering.

- [ ] **Step 5: Commit the domain foundation**

```powershell
git add src/LightTranslator/Models src/LightTranslator/Services/ScreenCapture/DpiCoordinateMapper.cs tests/LightTranslator.Tests/DpiCoordinateMapperTests.cs tests/LightTranslator.Tests/OcrBlockTests.cs
git commit -m "feat: add screenshot pixel coordinate model"
```

### Task 2: Current-monitor frozen capture and selection

**Files:**
- Create: `src/LightTranslator/Services/ScreenCapture/IDisplayCaptureService.cs`
- Create: `src/LightTranslator/Services/ScreenCapture/DisplayCaptureService.cs`
- Create: `src/LightTranslator/Services/Screenshot/IScreenshotCaptureView.cs`
- Modify: `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml`
- Modify: `src/LightTranslator/Views/ScreenshotCaptureWindow.xaml.cs`
- Create: `tests/LightTranslator.Tests/DisplayCaptureServiceTests.cs`

**Interfaces:**
- Consumes: `PixelRect`, `ScreenCaptureFrame`, `CapturedSelection`, and `DpiCoordinateMapper` from Task 1.
- Produces: `IDisplayCaptureService.CaptureMonitorAtCursor()`, `IDisplayCaptureService.Crop(ScreenCaptureFrame, PixelRect)`, and `IScreenshotCaptureView.SelectAsync(ScreenCaptureFrame, CancellationToken)`.

- [ ] **Step 1: Add failing capture/crop boundary tests**

```csharp
[Fact]
public void Crop_RejectsSelectionOutsideCurrentMonitor()
{
    var frame = TestImages.Frame(width: 800, height: 600, screenX: -800, screenY: 0, dpi: 120);
    var service = new DisplayCaptureService(new FakeDisplayNativeApi());

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        service.Crop(frame, new PixelRect(790, 10, 20, 20)));
}

[Fact]
public void Crop_PreservesAbsoluteScreenBoundsAndDpi()
{
    var frame = TestImages.Frame(width: 800, height: 600, screenX: -800, screenY: 100, dpi: 120);
    var service = new DisplayCaptureService(new FakeDisplayNativeApi());

    var result = service.Crop(frame, new PixelRect(100, 50, 200, 80));

    Assert.Equal(new PixelRect(-700, 150, 200, 80), result.ScreenBounds);
    Assert.Equal(120, result.DpiX);
    Assert.Equal(200, result.Image.PixelWidth);
    Assert.Equal(80, result.Image.PixelHeight);
}
```

- [ ] **Step 2: Run the focused tests and confirm they fail**

Run: `dotnet test --filter DisplayCaptureServiceTests`

Expected: build fails because the display capture contract and implementation do not exist.

- [ ] **Step 3: Implement memory-only monitor capture and crop**

```csharp
public interface IDisplayCaptureService
{
    ScreenCaptureFrame CaptureMonitorAtCursor();
    CapturedSelection Crop(ScreenCaptureFrame frame, PixelRect monitorRelativeBounds);
}

public interface IScreenshotCaptureView
{
    Task<PixelRect?> SelectAsync(
        ScreenCaptureFrame frame,
        CancellationToken cancellationToken = default);
}
```

`DisplayCaptureService.CaptureMonitorAtCursor()` must use `System.Windows.Forms.Cursor.Position`, `Screen.FromPoint`, and `Graphics.CopyFromScreen`; convert the in-memory bitmap to a frozen `BitmapSource` and dispose the GDI bitmap immediately. Determine DPI from the target monitor window handle, falling back to 96 only when the DPI API is unavailable. `Crop` validates the region is fully inside `0..PixelWidth` and `0..PixelHeight` and creates a frozen `CroppedBitmap`.

Update `ScreenshotCaptureWindow` so it is positioned to the captured monitor's DIP bounds instead of maximizing over the virtual desktop. Its background is the frozen `ScreenCaptureFrame.Image`; the existing drag rectangle remains on a dim layer. Convert the drag `Rect` back to physical pixels through `DpiCoordinateMapper.DipsToPixels`. `Esc` and cancellation close the window and return `null`.

- [ ] **Step 4: Run capture and existing screenshot tests**

Run: `dotnet test --filter "DisplayCaptureServiceTests|ScreenshotTranslationWindowTests|AppControllerTests"`

Expected: PASS; the crop is memory-only and rejects cross-monitor coordinates.

- [ ] **Step 5: Commit current-monitor capture**

```powershell
git add src/LightTranslator/Services/ScreenCapture src/LightTranslator/Services/Screenshot/IScreenshotCaptureView.cs src/LightTranslator/Views/ScreenshotCaptureWindow.xaml src/LightTranslator/Views/ScreenshotCaptureWindow.xaml.cs tests/LightTranslator.Tests/DisplayCaptureServiceTests.cs
git commit -m "feat: capture and select current monitor region"
```

### Task 3: Bundle and validate the universal PP-OCRv5 model

**Files:**
- Create: `tools/prepare_ocr_models.py`
- Create: `tools/test_prepare_ocr_models.py`
- Create: `src/LightTranslator/Assets/Ocr/ppocrv5_mobile_det.onnx`
- Create: `src/LightTranslator/Assets/Ocr/ppocrv5_mobile_rec.onnx`
- Create: `src/LightTranslator/Assets/Ocr/ppocrv5_dict.txt`
- Create: `src/LightTranslator/Assets/Ocr/THIRD-PARTY-NOTICES.md`
- Create: `src/LightTranslator/Services/Ocr/OcrModelPaths.cs`
- Create: `src/LightTranslator/Services/Ocr/OcrModelException.cs`
- Create: `src/LightTranslator/Services/Ocr/IOcrModelProvider.cs`
- Create: `src/LightTranslator/Services/Ocr/OcrModelProvider.cs`
- Modify: `src/LightTranslator/LightTranslator.csproj`
- Create: `tests/LightTranslator.Tests/OcrModelProviderTests.cs`

**Interfaces:**
- Consumes: application base directory.
- Produces: `IOcrModelProvider.GetRequiredPaths()` returning readable detector, recognizer, and dictionary paths; content copied to build and publish outputs.

- [ ] **Step 1: Add failing provider and publish-metadata tests**

```csharp
[Fact]
public void GetRequiredPaths_WhenFilesExist_ReturnsAllBundledAssets()
{
    using var directory = new TemporaryDirectory();
    directory.Write("Assets/Ocr/ppocrv5_mobile_det.onnx", new byte[] { 1 });
    directory.Write("Assets/Ocr/ppocrv5_mobile_rec.onnx", new byte[] { 2 });
    directory.WriteText("Assets/Ocr/ppocrv5_dict.txt", "blank\na\n");

    var paths = new OcrModelProvider(directory.Path).GetRequiredPaths();

    Assert.True(File.Exists(paths.DetectionModelPath));
    Assert.True(File.Exists(paths.RecognitionModelPath));
    Assert.True(File.Exists(paths.DictionaryPath));
}

[Fact]
public void GetRequiredPaths_WhenAssetMissing_ThrowsModelUnavailable()
{
    using var directory = new TemporaryDirectory();
    var error = Assert.Throws<OcrModelException>(() =>
        new OcrModelProvider(directory.Path).GetRequiredPaths());
    Assert.Equal("ModelUnavailable", error.ErrorCode);
}
```

- [ ] **Step 2: Run the provider test and confirm it fails**

Run: `dotnet test --filter OcrModelProviderTests`

Expected: build fails because `OcrModelProvider` and related types do not exist.

- [ ] **Step 3: Add the dependency, provider, and reproducible asset preparation**

Add to `LightTranslator.csproj`:

```xml
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.30.0" />

<Content Include="Assets\Ocr\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
</Content>
```

`tools/prepare_ocr_models.py` must:

1. download pinned revisions of `PaddlePaddle/PP-OCRv5_mobile_det_onnx` and `PaddlePaddle/PP-OCRv5_mobile_rec_onnx` with `huggingface_hub.snapshot_download`;
2. locate `inference.onnx` and verify it against the pinned upstream SHA-256 before staging it;
3. download the matching recognition dictionary from a pinned PaddleOCR revision;
4. write only the three runtime assets under `src/LightTranslator/Assets/Ocr`;
5. print SHA-256 values used verbatim in `THIRD-PARTY-NOTICES.md`.

The preparation command is development-only:

```powershell
python -m venv .model-tools
.\.model-tools\Scripts\python.exe -m pip install "huggingface_hub==0.34.4"
.\.model-tools\Scripts\python.exe -m unittest -v .\tools\test_prepare_ocr_models.py
.\.model-tools\Scripts\python.exe .\tools\prepare_ocr_models.py
```

The provider implementation uses `AppContext.BaseDirectory`, verifies each file exists, is non-empty, and is readable, and throws `new OcrModelException("ModelUnavailable", innerException)` without embedding paths in its public message.

- [ ] **Step 4: Run model-provider and clean-publish gates**

Run:

```powershell
.\.model-tools\Scripts\python.exe -m unittest -v .\tools\test_prepare_ocr_models.py
dotnet test --filter OcrModelProviderTests
dotnet publish .\src\LightTranslator\LightTranslator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o .\publish\phase2a-model-gate
Get-ChildItem .\publish\phase2a-model-gate\Assets\Ocr
```

Expected: tests PASS; publish output contains both ONNX files, dictionary, and notice; there is no Python executable, virtual environment, Paddle framework, or model download cache in the publish directory.

- [ ] **Step 5: Commit the verified model assets**

```powershell
git add tools/prepare_ocr_models.py src/LightTranslator/Assets/Ocr src/LightTranslator/Services/Ocr src/LightTranslator/LightTranslator.csproj tests/LightTranslator.Tests/OcrModelProviderTests.cs
git commit -m "feat: bundle universal PP-OCRv5 models"
```

### Task 4: ONNX OCR preprocessing, decoding, and inference

**Files:**
- Create: `src/LightTranslator/Services/Ocr/IOcrService.cs`
- Create: `src/LightTranslator/Services/Ocr/OcrImagePreprocessor.cs`
- Create: `src/LightTranslator/Services/Ocr/DbDetectorPostProcessor.cs`
- Create: `src/LightTranslator/Services/Ocr/CtcTextDecoder.cs`
- Create: `src/LightTranslator/Services/Ocr/PaddleOcrService.cs`
- Create: `tests/LightTranslator.Tests/OcrImagePreprocessorTests.cs`
- Create: `tests/LightTranslator.Tests/DbDetectorPostProcessorTests.cs`
- Create: `tests/LightTranslator.Tests/CtcTextDecoderTests.cs`
- Create: `tests/LightTranslator.Tests/PaddleOcrServiceTests.cs`

**Interfaces:**
- Consumes: `IOcrModelProvider`, frozen `BitmapSource`, `OcrBlock`, and cancellation tokens.
- Produces: `IOcrService.RecognizeAsync(BitmapSource, CancellationToken)` returning filtered, ordered OCR blocks.

- [ ] **Step 1: Add failing pure algorithm tests**

```csharp
[Fact]
public void Decode_CollapsesRepeatsAndBlankTokens()
{
    var decoder = new CtcTextDecoder(new[] { "", "你", "好" });
    var logits = TestLogits.ArgMaxSequence(0, 1, 1, 0, 2, 2, 0);

    var result = decoder.Decode(logits);

    Assert.Equal("你好", result.Text);
    Assert.InRange(result.Confidence, 0.99, 1.0);
}

[Fact]
public void Process_RejectsWeakBoxesAndClipsToImage()
{
    var map = TestProbabilityMaps.WithRectangle(20, 10, 80, 30, probability: 0.90f);
    var processor = new DbDetectorPostProcessor(
        pixelThreshold: 0.30f,
        boxThreshold: 0.60f,
        unclipRatio: 1.50f);

    var boxes = processor.Process(map, imageWidth: 100, imageHeight: 50);

    var box = Assert.Single(boxes);
    Assert.True(box.X >= 0 && box.Y >= 0);
    Assert.True(box.X + box.Width <= 100);
    Assert.True(box.Y + box.Height <= 50);
}
```

Add preprocessing tests that assert detector output is RGB NCHW, resized to multiples of 32 with max side 960, and recognizer crops are height 48 with padded width capped at 320.

- [ ] **Step 2: Run OCR algorithm tests and confirm they fail**

Run: `dotnet test --filter "OcrImagePreprocessorTests|DbDetectorPostProcessorTests|CtcTextDecoderTests|PaddleOcrServiceTests"`

Expected: build fails because OCR algorithm and service types do not exist.

- [ ] **Step 3: Implement the pure algorithms and ONNX service**

```csharp
public interface IOcrService
{
    Task<IReadOnlyList<OcrBlock>> RecognizeAsync(
        BitmapSource image,
        CancellationToken cancellationToken = default);
}
```

Detector preprocessing: RGB, NCHW, max side 960, dimensions rounded to multiples of 32, values normalized with scale `1/255`, mean `(0.485, 0.456, 0.406)`, and standard deviation `(0.229, 0.224, 0.225)`. Detector postprocessing uses pixel threshold 0.30, box threshold 0.60, and unclip ratio 1.50, then returns clipped axis-aligned rectangles.

Recognition preprocessing: crop each detector rectangle, resize to height 48 while preserving aspect ratio, cap/pad width at 320, RGB NCHW, normalize each channel with `(value / 255 - 0.5) / 0.5`. `CtcTextDecoder` treats index 0 as blank, collapses consecutive duplicate indexes, and averages emitted-token probabilities for confidence.

`PaddleOcrService` creates one detector and one recognizer `InferenceSession` lazily and reuses them. Obtain input names from `session.InputMetadata.Keys.Single()` and output tensors from the single output value instead of hard-coding exported node names. Check cancellation before detector inference, between crops, and before returning. Convert retained detections to IDs `block-0001`, `block-0002`, and so on, apply `OcrBlock.FilterAndSort(..., 0.50)`, and do not log text or images.

- [ ] **Step 4: Run algorithm, model smoke, and cancellation tests**

Run: `dotnet test --filter "OcrImagePreprocessorTests|DbDetectorPostProcessorTests|CtcTextDecoderTests|PaddleOcrServiceTests"`

Expected: PASS; the real bundled-model smoke test opens both sessions, verifies rank-4 float inputs, and recognizes at least one block from a generated `Hello 你好 日本語` image. Cancellation produces `OperationCanceledException`, not an OCR error.

- [ ] **Step 5: Commit the local OCR engine**

```powershell
git add src/LightTranslator/Services/Ocr tests/LightTranslator.Tests/OcrImagePreprocessorTests.cs tests/LightTranslator.Tests/DbDetectorPostProcessorTests.cs tests/LightTranslator.Tests/CtcTextDecoderTests.cs tests/LightTranslator.Tests/PaddleOcrServiceTests.cs
git commit -m "feat: run PP-OCRv5 through ONNX Runtime"
```

### Task 5: Privacy-safe numbered batch translation

**Files:**
- Create: `src/LightTranslator/Services/Translation/IScreenshotTextTranslator.cs`
- Create: `src/LightTranslator/Services/Translation/DeepSeekScreenshotTextTranslator.cs`
- Create: `tests/LightTranslator.Tests/DeepSeekScreenshotTextTranslatorTests.cs`

**Interfaces:**
- Consumes: ordered `IReadOnlyList<OcrBlock>`, source language, target language, API key from the existing `ISecretStorage`, and `HttpClient`.
- Produces: `TranslateAsync(...)` returning `IReadOnlyDictionary<string, string>` keyed by stable block ID.

- [ ] **Step 1: Add failing request privacy and response mapping tests**

```csharp
[Fact]
public async Task TranslateAsync_SendsOnlyIdsTextAndLanguages()
{
    var handler = new RecordingHandler(JsonResponse("""
        {"choices":[{"message":{"content":"{\"translations\":[{\"id\":\"block-0001\",\"text\":\"你好\"}]}"}}]}
        """));
    var sut = CreateTranslator(handler, apiKey: "sk-private-value");
    var blocks = new[] { new OcrBlock("block-0001", "Hello", 0.99, new PixelRect(10, 20, 80, 30)) };

    var result = await sut.TranslateAsync(blocks, "en", "zh");

    Assert.Equal("你好", result["block-0001"]);
    Assert.Contains("block-0001", handler.Body);
    Assert.Contains("Hello", handler.Body);
    Assert.DoesNotContain("\"Bounds\"", handler.Body);
    Assert.DoesNotContain("10", handler.Body);
    Assert.DoesNotContain("sk-private-value", handler.Body);
}

[Fact]
public async Task TranslateAsync_MissingResponseId_DoesNotShiftTranslations()
{
    var sut = CreateTranslator(responseTranslations: new[]
    {
        new { id = "block-0002", text = "第二" }
    });

    var result = await sut.TranslateAsync(TwoBlocks(), "auto", "zh");

    Assert.False(result.ContainsKey("block-0001"));
    Assert.Equal("第二", result["block-0002"]);
}
```

- [ ] **Step 2: Run the focused tests and confirm they fail**

Run: `dotnet test --filter DeepSeekScreenshotTextTranslatorTests`

Expected: build fails because the screenshot batch translator does not exist.

- [ ] **Step 3: Implement the dedicated batch protocol**

```csharp
public interface IScreenshotTextTranslator
{
    Task<IReadOnlyDictionary<string, string>> TranslateAsync(
        IReadOnlyList<OcrBlock> blocks,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}
```

Send one JSON array whose items contain exactly `id` and `text`. The system prompt requires strict JSON `{ "translations": [{ "id": "...", "text": "..." }] }`, preserves IDs exactly, and uses the requested source/target language. Accept only IDs present in the request, ignore duplicates after the first valid value, ignore missing IDs, and never remap by array position. Reuse existing `TranslationException` kinds for missing key, timeout, rate limit, authentication, invalid response, and network failure. Do not replace the Phase 1 single-text `ITranslationService`.

- [ ] **Step 4: Run batch translation and Phase 1 translation tests**

Run: `dotnet test --filter "DeepSeekScreenshotTextTranslatorTests|DeepSeekTranslationServiceTests|LoggingTranslationServiceTests"`

Expected: PASS; Phase 1 translation behavior remains unchanged.

- [ ] **Step 5: Commit batch translation**

```powershell
git add src/LightTranslator/Services/Translation/IScreenshotTextTranslator.cs src/LightTranslator/Services/Translation/DeepSeekScreenshotTextTranslator.cs tests/LightTranslator.Tests/DeepSeekScreenshotTextTranslatorTests.cs
git commit -m "feat: translate numbered OCR blocks in one request"
```

### Task 6: In-place result overlay and close gestures

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/IScreenshotResultView.cs`
- Create: `src/LightTranslator/Services/Screenshot/IScreenshotResultViewFactory.cs`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml`
- Modify: `src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs`
- Modify: `tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs`

**Interfaces:**
- Consumes: `CapturedSelection`, translated `OcrBlock` values, and `DpiCoordinateMapper`.
- Produces: a topmost window with `ShowLoading`, `ShowResults`, `ShowMessage`, `Close`, and `CloseRequested`.

- [ ] **Step 1: Replace the placeholder assertion with failing result-state tests**

```csharp
[Fact]
public void ShowResults_RendersOnlyTranslatedTextAtMappedBounds()
{
    StaTest.Run(() =>
    {
        var window = CreateWindow(width: 400, height: 200, dpi: 120);
        window.ShowResults(new[]
        {
            new OcrBlock("block-0001", "Secret source", 0.9,
                new PixelRect(100, 50, 200, 40), "译文")
        });

        var block = Assert.Single(window.TranslationCanvas.Children.OfType<Border>());
        Assert.Equal(80, Canvas.GetLeft(block));
        Assert.Equal(40, Canvas.GetTop(block));
        Assert.Equal("译文", ((TextBlock)block.Child).Text);
        Assert.DoesNotContain("Secret source", window.VisibleText());
    });
}

[Theory]
[InlineData("Escape")]
[InlineData("Mouse")]
[InlineData("AltQ")]
public void CloseGesture_RaisesOneCloseRequest(string gesture)
{
    StaTest.Run(() =>
    {
        var window = CreateWindow();
        var count = 0;
        window.CloseRequested += (_, _) => count++;
        window.RaiseGestureForTest(gesture);
        Assert.Equal(1, count);
    });
}
```

- [ ] **Step 2: Run window tests and confirm they fail against the placeholder**

Run: `dotnet test --filter ScreenshotTranslationWindowTests`

Expected: FAIL because the placeholder has no frozen image, translation canvas, or result-state API.

- [ ] **Step 3: Implement loading, result, and error presentation**

```csharp
public interface IScreenshotResultView
{
    event EventHandler? CloseRequested;
    void ShowLoading(CapturedSelection selection, string message);
    void ShowResults(IReadOnlyList<OcrBlock> blocks);
    void ShowMessage(string message);
    void Close();
}

public interface IScreenshotResultViewFactory
{
    IScreenshotResultView Create(CapturedSelection selection);
}
```

Position the borderless `Topmost="True"` window at `CapturedSelection.ScreenBounds` converted to DIPs. Keep the crop as the background for loading, result, and error states. Render only non-empty `TranslatedText` using dark backgrounds at 78% opacity and light wrapped text. Convert each OCR pixel rectangle to DIPs. Mouse-down anywhere, `Esc`, and `Alt+Q` raise `CloseRequested` once; there is no timer and no retry button.

- [ ] **Step 4: Run the STA window tests**

Run: `dotnet test --filter ScreenshotTranslationWindowTests`

Expected: PASS for loading, no-text/error messages, translated-only rendering, topmost state, coordinate mapping, and all close gestures.

- [ ] **Step 5: Commit the result overlay**

```powershell
git add src/LightTranslator/Services/Screenshot/IScreenshotResultView.cs src/LightTranslator/Services/Screenshot/IScreenshotResultViewFactory.cs src/LightTranslator/Views/ScreenshotTranslationWindow.xaml src/LightTranslator/Views/ScreenshotTranslationWindow.xaml.cs tests/LightTranslator.Tests/ScreenshotTranslationWindowTests.cs
git commit -m "feat: render screenshot translations in place"
```

### Task 7: Single-flight workflow coordinator and cancellation

**Files:**
- Create: `src/LightTranslator/Services/Screenshot/ScreenshotTranslationCoordinator.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotTranslationCoordinatorTests.cs`
- Modify: `src/LightTranslator/Controllers/IScreenshotTranslationView.cs`
- Modify: `tests/LightTranslator.Tests/AppControllerTests.cs`

**Interfaces:**
- Consumes: `IDisplayCaptureService`, `IScreenshotCaptureView`, `IOcrService`, `IScreenshotTextTranslator`, `IScreenshotResultViewFactory`, `ISettingsService`, and `AppLogger`.
- Produces: `ScreenshotTranslationCoordinator.Toggle()` and `ScreenshotTranslationCoordinator.Dispose()`; `IScreenshotTranslationView.ShowScreenshotTranslation()` keeps its existing controller-facing signature and delegates to `Toggle()`.

- [ ] **Step 1: Add failing state, error, and late-result tests**

```csharp
[Fact]
public async Task Toggle_WhenTaskActive_CancelsAndClosesInsteadOfStartingAnother()
{
    var fixture = CoordinatorFixture.WithPendingOcr();
    fixture.Coordinator.Toggle();
    await fixture.WaitUntilResultWindowShown();

    fixture.Coordinator.Toggle();

    Assert.True(fixture.OcrCancellationToken.IsCancellationRequested);
    Assert.Equal(1, fixture.CaptureView.CallCount);
    Assert.Equal(1, fixture.ResultView.CloseCount);
}

[Fact]
public async Task CancelledLateTranslation_CannotReopenOrUpdateWindow()
{
    var fixture = CoordinatorFixture.WithPendingTranslationIgnoringCancellation();
    fixture.Coordinator.Toggle();
    await fixture.WaitUntilTranslationStarted();
    fixture.Coordinator.Toggle();
    fixture.CompleteTranslation(new Dictionary<string, string> { ["block-0001"] = "迟到" });

    await fixture.DrainAsync();

    Assert.Equal(0, fixture.ResultView.ShowResultsCount);
    Assert.Equal(1, fixture.ResultView.CloseCount);
}

[Theory]
[InlineData("NoText", "未识别到文字")]
[InlineData("Ocr", "文字识别失败，请重试")]
[InlineData("MissingApiKey", "请先在设置中配置 API Key")]
[InlineData("Network", "翻译失败，请检查网络后重试")]
public async Task Failure_ShowsExpectedOverlayMessage(string failure, string expected)
{
    var fixture = CoordinatorFixture.FailingAt(failure);
    fixture.Coordinator.Toggle();
    await fixture.DrainAsync();
    Assert.Equal(expected, fixture.ResultView.LastMessage);
}
```

- [ ] **Step 2: Run coordinator tests and confirm they fail**

Run: `dotnet test --filter "ScreenshotTranslationCoordinatorTests|AppControllerTests"`

Expected: build fails because the coordinator does not exist.

- [ ] **Step 3: Implement generation-guarded single-flight orchestration**

`Toggle()` behavior:

1. if an active cancellation source exists, cancel it, close capture/result views, increment a generation number, clear active state, and return;
2. capture the cursor monitor before opening selection UI;
3. await selection; `null` ends silently;
4. crop the frame, create result view, subscribe to `CloseRequested`, and call `ShowLoading(selection, "正在识别…")` immediately;
5. run OCR off the UI thread; an empty result calls `ShowMessage("未识别到文字")`;
6. read screenshot source/target language from settings and batch-translate retained OCR blocks;
7. merge translations strictly by block ID and call `ShowResults` only if the captured generation is still active;
8. map known failures to the exact Chinese messages in the tests; cancellation is silent;
9. log only stage name, elapsed milliseconds, block count, `ppocrv5-mobile-universal`, completion/cancellation state, and exception type.

Every awaited boundary calls `IsCurrent(generation, token)` before touching the view. `Dispose()` cancels active work and disposes both ONNX sessions through `IOcrService` when it implements `IDisposable`.

- [ ] **Step 4: Run coordinator, controller, and race tests**

Run: `dotnet test --filter "ScreenshotTranslationCoordinatorTests|AppControllerTests"`

Expected: PASS; a second trigger closes instead of recapturing, user close cancels, and late results are ignored.

- [ ] **Step 5: Commit the workflow coordinator**

```powershell
git add src/LightTranslator/Services/Screenshot/ScreenshotTranslationCoordinator.cs src/LightTranslator/Controllers/IScreenshotTranslationView.cs tests/LightTranslator.Tests/ScreenshotTranslationCoordinatorTests.cs tests/LightTranslator.Tests/AppControllerTests.cs
git commit -m "feat: coordinate cancellable screenshot translation"
```

### Task 8: Screenshot language settings

**Files:**
- Create: `src/LightTranslator/Services/Settings/IScreenshotLanguageSettingsPersistence.cs`
- Create: `src/LightTranslator/Services/Settings/ScreenshotLanguageSettingsPersistence.cs`
- Modify: `src/LightTranslator/ViewModels/FirstRunSettingsViewModel.cs`
- Modify: `src/LightTranslator/Views/FirstRunSettingsWindow.xaml`
- Modify: `src/LightTranslator/Views/FirstRunSettingsWindow.xaml.cs`
- Create: `tests/LightTranslator.Tests/ScreenshotLanguageSettingsPersistenceTests.cs`
- Modify: `tests/LightTranslator.Tests/FirstRunSettingsViewModelTests.cs`
- Modify: `tests/LightTranslator.Tests/FirstRunSettingsWindowTests.cs`

**Interfaces:**
- Consumes: existing `AppSettings.ScreenshotSourceLanguage`, `AppSettings.ScreenshotTargetLanguage`, `ISettingsService`, and `LanguageOption`.
- Produces: persisted screenshot source options `auto/zh/en/ja` and target options `zh/en/ja` available in first-run and normal settings.

- [ ] **Step 1: Add failing persistence and UI binding tests**

```csharp
[Fact]
public void Save_UpdatesOnlyScreenshotLanguages()
{
    var settings = TestSettings.Create(textSource: "en", textTarget: "zh");
    var service = new ScreenshotLanguageSettingsPersistence(new InMemorySettingsService(settings));

    service.Save("ja", "en");

    Assert.Equal("ja", settings.ScreenshotSourceLanguage);
    Assert.Equal("en", settings.ScreenshotTargetLanguage);
    Assert.Equal("en", settings.TextSourceLanguage);
    Assert.Equal("zh", settings.TextTargetLanguage);
}

[Fact]
public void ViewModel_ExposesUniversalModelLanguages()
{
    var vm = CreateViewModel(screenshotSource: "auto", screenshotTarget: "zh");

    Assert.Equal(new[] { "auto", "zh", "en", "ja" },
        vm.ScreenshotSourceLanguages.Select(x => x.Code));
    Assert.Equal(new[] { "zh", "en", "ja" },
        vm.ScreenshotTargetLanguages.Select(x => x.Code));
}
```

- [ ] **Step 2: Run screenshot settings tests and confirm they fail**

Run: `dotnet test --filter "ScreenshotLanguageSettingsPersistenceTests|FirstRunSettingsViewModelTests|FirstRunSettingsWindowTests"`

Expected: FAIL because screenshot language persistence and selectors are absent.

- [ ] **Step 3: Implement independent screenshot-language persistence and selectors**

```csharp
public interface IScreenshotLanguageSettingsPersistence
{
    void Save(string sourceLanguage, string targetLanguage);
}
```

Reject source codes outside `auto`, `zh`, `en`, `ja`; reject target codes outside `zh`, `en`, `ja`; reject identical explicit source/target pairs. Extend the existing settings view model constructor with `IScreenshotLanguageSettingsPersistence`, expose collections and selected properties, and save screenshot languages in the same successful settings transaction as the existing values. Add two labeled combo boxes under a “截图翻译” section without changing the text-translation selectors.

- [ ] **Step 4: Run all settings tests**

Run: `dotnet test --filter "Settings|FirstRunSettings"`

Expected: PASS; reopening settings restores screenshot values and Phase 1 values remain unchanged.

- [ ] **Step 5: Commit screenshot language settings**

```powershell
git add src/LightTranslator/Services/Settings src/LightTranslator/ViewModels/FirstRunSettingsViewModel.cs src/LightTranslator/Views/FirstRunSettingsWindow.xaml src/LightTranslator/Views/FirstRunSettingsWindow.xaml.cs tests/LightTranslator.Tests/ScreenshotLanguageSettingsPersistenceTests.cs tests/LightTranslator.Tests/FirstRunSettingsViewModelTests.cs tests/LightTranslator.Tests/FirstRunSettingsWindowTests.cs
git commit -m "feat: configure screenshot translation languages"
```

### Task 9: Application composition and privacy-safe logging

**Files:**
- Modify: `src/LightTranslator/App.xaml.cs`
- Modify: `src/LightTranslator/Services/Logging/AppLogger.cs`
- Modify: `tests/LightTranslator.Tests/AppLoggerTests.cs`
- Modify: `tests/LightTranslator.Tests/AppControllerTests.cs`

**Interfaces:**
- Consumes: all Phase 2A services from Tasks 2–8 and existing tray/hotkey controller events.
- Produces: both `Alt+Q` and tray “截图翻译” invoking the same coordinator; deterministic application shutdown.

- [ ] **Step 1: Add failing composition and log-redaction tests**

```csharp
[Fact]
public void ScreenshotMetric_DoesNotWriteContentOrSecrets()
{
    var sink = new RecordingLogSink();
    var logger = new AppLogger(sink);

    logger.ScreenshotStageCompleted(
        stage: "translation",
        elapsedMilliseconds: 123,
        blockCount: 4,
        modelId: "ppocrv5-mobile-universal");

    Assert.Contains("translation", sink.LastMessage);
    Assert.Contains("123", sink.LastMessage);
    Assert.Contains("4", sink.LastMessage);
    Assert.DoesNotContain("Hello", sink.LastMessage);
    Assert.DoesNotContain("你好", sink.LastMessage);
    Assert.DoesNotContain("sk-", sink.LastMessage);
}
```

Add an `AppControllerTests` case that fires both screenshot hotkey and tray events and asserts they reach the same `IScreenshotTranslationView.ShowScreenshotTranslation()` boundary.

- [ ] **Step 2: Run logging and controller tests and confirm the new metric fails**

Run: `dotnet test --filter "AppLoggerTests|AppControllerTests|TrayServiceTests|HotkeyServiceTests"`

Expected: FAIL because `ScreenshotStageCompleted` and the Phase 2A composition are not present.

- [ ] **Step 3: Wire services and add structured, content-free metrics**

In `App.OnStartup`, construct one each of `DisplayCaptureService`, `OcrModelProvider`, `PaddleOcrService`, `DeepSeekScreenshotTextTranslator`, screenshot view factories, and `ScreenshotTranslationCoordinator`. Retain existing `AppController` event wiring; implement `App.ShowScreenshotTranslation()` as `_screenshotCoordinator.Toggle()`. Share the existing `HttpClient`, secret storage, settings service, and logger. In `OnExit`, dispose the coordinator/OCR sessions before existing tray, hotkey, and HTTP resources.

Add only fixed-field logger methods:

```csharp
public void ScreenshotStageCompleted(
    string stage,
    long elapsedMilliseconds,
    int blockCount,
    string modelId);

public void ScreenshotStageFailed(
    string stage,
    string exceptionType);
```

Callers pass allow-listed stage names (`capture`, `ocr`, `translation`, `render`) and `exception.GetType().Name`; no free-form exception message is written.

- [ ] **Step 4: Run composition, privacy, and Phase 1 entry-point tests**

Run: `dotnet test --filter "AppLoggerTests|AppControllerTests|TrayServiceTests|HotkeyServiceTests|NotifyIconTrayBackendTests"`

Expected: PASS; tray and `Alt+Q` use one coordinator and logs contain no user content.

- [ ] **Step 5: Commit application composition**

```powershell
git add src/LightTranslator/App.xaml.cs src/LightTranslator/Services/Logging/AppLogger.cs tests/LightTranslator.Tests/AppLoggerTests.cs tests/LightTranslator.Tests/AppControllerTests.cs
git commit -m "feat: wire screenshot translation workflow"
```

### Task 10: Full verification, self-contained package, and Phase 2A acceptance

**Files:**
- Create: `docs/phase2a-acceptance.md`
- Modify only if a gate exposes a defect: the smallest production/test file responsible for that defect.

**Interfaces:**
- Consumes: completed Phase 2A workflow and Phase 1 regression suite.
- Produces: verified Release build, self-contained package, hash, and recorded manual acceptance evidence.

- [ ] **Step 1: Run the complete automated gate**

Run:

```powershell
dotnet test -c Release
dotnet build -c Release
git diff --check
git status --short
```

Expected: all tests pass, build has 0 warnings and 0 errors, whitespace check is clean, and status contains only the intended acceptance document before its commit.

- [ ] **Step 2: Publish to a fresh directory and inspect contents**

Run:

```powershell
$publishDir = ".\publish\LightTranslator-Phase2A-win-x64"
dotnet publish ".\src\LightTranslator\LightTranslator.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $publishDir
Get-ChildItem $publishDir -Recurse
Select-String -Path "$publishDir\*" -Pattern "sk-[A-Za-z0-9_-]{10,}" -ErrorAction SilentlyContinue
```

Expected: application and native WPF/ONNX runtime files are present; `Assets\Ocr` contains the two models, dictionary, and notice; secret scan finds no API key; there is no `.pdb` in the distribution archive.

- [ ] **Step 3: Perform and record manual acceptance**

Create `docs/phase2a-acceptance.md` with a result column for each exact check:

- `Alt+Q` and tray menu both complete selection → OCR → translation → in-place overlay;
- click, `Esc`, and second `Alt+Q` close the overlay;
- second trigger during OCR/translation cancels without a late window;
- pure Chinese, pure English, Japanese, and mixed Chinese/English recognition;
- no-text, missing-key, invalid-key, network-offline, and OCR-model-missing messages;
- primary and secondary monitor selection, including a monitor left of the primary;
- Windows scaling 100%, 125%, and 150%;
- fresh machine without .NET, Python, PaddleOCR, or ONNX Runtime installed;
- `%LOCALAPPDATA%\LightTranslator\settings.json` contains no API key;
- DPAPI secret exists only after that user enters a key;
- log search contains none of the acceptance sample source text, translation text, or API key.

- [ ] **Step 4: Create the distribution archive and checksum**

Run:

```powershell
Remove-Item "$publishDir\LightTranslator.pdb" -ErrorAction SilentlyContinue
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$zipPath = ".\publish\LightTranslator-Phase2A-win-x64-$stamp.zip"
Compress-Archive -Path $publishDir -DestinationPath $zipPath -CompressionLevel Optimal
Get-FileHash $zipPath -Algorithm SHA256
```

Expected: one timestamped ZIP and one recorded SHA-256 value; extraction preserves `Assets\Ocr` beside the executable.

- [ ] **Step 5: Commit acceptance evidence**

```powershell
git add docs/phase2a-acceptance.md
git commit -m "docs: record Phase 2A acceptance"
git status --short
```

Expected: clean working tree on `feature/phase2a-screenshot-translation`. Do not merge or tag until the user reviews the acceptance record.

---

## Plan Self-Review Record

- Spec coverage: every design section maps to Tasks 1–10, including current-monitor capture, DPI conversion, local universal OCR, stable-ID batch translation, translated-only overlay, cancellation, error states, privacy, performance instrumentation, settings, and clean-machine publishing.
- Scope boundary: no background repair, typography matching, cross-monitor selection, rotated/vertical text optimization, history, saving, editing, copy, export, or result retry control is introduced.
- Type consistency: `PixelRect`, `ScreenCaptureFrame`, `CapturedSelection`, `OcrBlock`, `IOcrService`, `IScreenshotTextTranslator`, and screenshot view contracts retain identical names and signatures across producer and consumer tasks.
- Model consistency: all four source codes (`auto`, `zh`, `en`, `ja`) resolve to the same bundled detector/recognizer; no download manager or per-language model selector exists.
- Verification order: pinned model acquisition and checksum validation are gated before OCR implementation; pure algorithms precede live inference; coordinator precedes composition; full regression and clean-machine acceptance are last.
