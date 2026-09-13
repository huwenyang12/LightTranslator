# LightTranslator Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a usable Windows 10/11 LightTranslator application that runs in the tray, supports configurable global hotkeys, securely stores a DeepSeek API key, and provides the complete `Alt + T` text-translation workflow.

**Architecture:** One production WPF project (`LightTranslator`) contains Views, ViewModels, Services, Models, and Windows-specific Infrastructure. A separate xUnit test project exists only for automated tests; production code remains a single WPF project. `AppController` coordinates startup, tray, hotkeys, and `WindowManager`; UI depends on interfaces rather than DeepSeek/Win32 details.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit, built-in `HttpClient`, Windows `RegisterHotKey`, `System.Windows.Forms.NotifyIcon`, Windows DPAPI (`ProtectedData`).

**Spec:** `docs/superpowers/specs/2026-09-13-lighttranslator-v1-design.md`

## Global Constraints

- Production target: Windows 10 / Windows 11.
- Production code is one `.NET 8` WPF project; the only additional project is `LightTranslator.Tests`.
- Default text-translation hotkey: `Alt + T`.
- Default screenshot hotkey remains reserved as `Alt + Q`, but screenshot translation is Phase 2.
- Text translation supports source languages `Auto`, `zh`, `en`, `ja`; target languages `zh`, `en`, `ja`.
- Input translation debounce: `400ms`.
- `Enter`: copy current translation and close.
- `Shift + Enter`: insert newline.
- `Esc` or `Alt + T`: close without copying.
- No translation history.
- Do not log user input, translation text, screenshots, or API keys.
- DeepSeek API key must never be stored in plaintext.
- UI work must not block the WPF dispatcher thread.
- Do not add OCR, OpenCV, screenshot capture, or Overlay code in Phase 1.

---

## File Structure Locked for Phase 1

```text
LightTranslator/
├─ LightTranslator.sln
├─ docs/
│  └─ superpowers/
│     ├─ specs/
│     │  └─ 2026-09-13-lighttranslator-v1-design.md
│     └─ plans/
│        └─ 2026-09-13-lighttranslator-phase1-plan.md
├─ src/
│  └─ LightTranslator/
│     ├─ LightTranslator.csproj
│     ├─ App.xaml
│     ├─ App.xaml.cs
│     ├─ AppController.cs
│     ├─ Models/
│     │  ├─ AppSettings.cs
│     │  ├─ HotkeyDefinition.cs
│     │  ├─ LanguageOption.cs
│     │  ├─ TranslationRequest.cs
│     │  └─ TranslationResult.cs
│     ├─ Services/
│     │  ├─ Clipboard/
│     │  │  ├─ IClipboardService.cs
│     │  │  └─ ClipboardService.cs
│     │  ├─ Hotkeys/
│     │  │  ├─ HotkeyAction.cs
│     │  │  ├─ HotkeyService.cs
│     │  │  ├─ IHotkeyBackend.cs
│     │  │  └─ NativeHotkeyBackend.cs
│     │  ├─ Settings/
│     │  │  ├─ ISettingsService.cs
│     │  │  └─ SettingsService.cs
│     │  ├─ Tray/
│     │  │  └─ TrayService.cs
│     │  ├─ Translation/
│     │  │  ├─ DeepSeekTranslationService.cs
│     │  │  ├─ ITranslationService.cs
│     │  │  └─ TranslationException.cs
│     │  └─ Windows/
│     │     └─ WindowManager.cs
│     ├─ Infrastructure/
│     │  ├─ Security/
│     │  │  ├─ ISecretStorage.cs
│     │  │  └─ DpapiSecretStorage.cs
│     │  └─ Windows/
│     │     └─ NativeMethods.cs
│     ├─ ViewModels/
│     │  ├─ SettingsViewModel.cs
│     │  └─ TranslateViewModel.cs
│     └─ Views/
│        ├─ SettingsWindow.xaml
│        ├─ SettingsWindow.xaml.cs
│        ├─ TranslateWindow.xaml
│        ├─ TranslateWindow.xaml.cs
│        ├─ WelcomeWindow.xaml
│        └─ WelcomeWindow.xaml.cs
└─ tests/
   └─ LightTranslator.Tests/
      ├─ LightTranslator.Tests.csproj
      ├─ SettingsServiceTests.cs
      ├─ DpapiSecretStorageTests.cs
      ├─ DeepSeekTranslationServiceTests.cs
      ├─ TranslateViewModelTests.cs
      └─ HotkeyServiceTests.cs
```

---

### Task 1: Bootstrap the WPF solution and establish the test harness

**Files:**

- Create: `LightTranslator.sln`
- Create: `src/LightTranslator/LightTranslator.csproj`
- Create: `tests/LightTranslator.Tests/LightTranslator.Tests.csproj`
- Create: `tests/LightTranslator.Tests/SmokeTests.cs`
- Create: `docs/superpowers/specs/2026-09-13-lighttranslator-v1-design.md`
- Create: `docs/superpowers/plans/2026-09-13-lighttranslator-phase1-plan.md`

**Interfaces:**

- Consumes: approved V1 design document.

- Produces: buildable solution and executable xUnit test project.

- [ ] **Step 1: Create the repository and WPF/test projects**

Run from the project parent directory:

```powershell
mkdir LightTranslator
cd LightTranslator
git init

dotnet new sln -n LightTranslator
dotnet new wpf -n LightTranslator -o src/LightTranslator -f net8.0
dotnet new xunit -n LightTranslator.Tests -o tests/LightTranslator.Tests -f net8.0

# Then edit tests/LightTranslator.Tests/LightTranslator.Tests.csproj and set:
# <TargetFramework>net8.0-windows</TargetFramework>

dotnet sln add src/LightTranslator/LightTranslator.csproj
dotnet sln add tests/LightTranslator.Tests/LightTranslator.Tests.csproj
dotnet add tests/LightTranslator.Tests/LightTranslator.Tests.csproj reference src/LightTranslator/LightTranslator.csproj
```

Expected: both projects are listed by `dotnet sln list`.

- [ ] **Step 2: Enable Windows Forms only for the tray icon**

Modify `src/LightTranslator/LightTranslator.csproj` so the main property group is:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UseWPF>true</UseWPF>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

- [ ] **Step 3: Add a failing smoke test**

Create `tests/LightTranslator.Tests/SmokeTests.cs`:

```csharp
namespace LightTranslator.Tests;

public class SmokeTests
{
    [Fact]
    public void ApplicationAssembly_IsLoadable()
    {
        var assembly = typeof(App).Assembly;

        Assert.Equal("LightTranslator", assembly.GetName().Name);
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test
```

Expected: PASS.

- [ ] **Step 5: Build the WPF application**

Run:

```powershell
dotnet build src/LightTranslator/LightTranslator.csproj
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 6: Copy the approved design and this plan into the repository**

Create:

```text
docs/superpowers/specs/2026-09-13-lighttranslator-v1-design.md
docs/superpowers/plans/2026-09-13-lighttranslator-phase1-plan.md
```

Use the approved design and plan content verbatim.

- [ ] **Step 7: Commit**

```powershell
git add .
git commit -m "chore: bootstrap LightTranslator WPF solution"
```

---

### Task 2: Implement settings persistence and DPAPI secret storage

**Files:**

- Create: `src/LightTranslator/Models/AppSettings.cs`
- Create: `src/LightTranslator/Models/HotkeyDefinition.cs`
- Create: `src/LightTranslator/Models/LanguageOption.cs`
- Create: `src/LightTranslator/Services/Settings/ISettingsService.cs`
- Create: `src/LightTranslator/Services/Settings/SettingsService.cs`
- Create: `src/LightTranslator/Infrastructure/Security/ISecretStorage.cs`
- Create: `src/LightTranslator/Infrastructure/Security/DpapiSecretStorage.cs`
- Test: `tests/LightTranslator.Tests/SettingsServiceTests.cs`
- Test: `tests/LightTranslator.Tests/DpapiSecretStorageTests.cs`

**Interfaces:**

- Consumes: file system and current Windows user profile.

- Produces:

  - `Task<AppSettings> ISettingsService.LoadAsync(CancellationToken cancellationToken = default)`
  - `Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)`
  - `void ISecretStorage.Save(string name, string secret)`
  - `string? Load(string name)`
  - `void Delete(string name)`

- [ ] **Step 1: Write failing settings tests**

Create `tests/LightTranslator.Tests/SettingsServiceTests.cs`:

```csharp
using LightTranslator.Models;
using LightTranslator.Services.Settings;

namespace LightTranslator.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SettingsService(dir);

        var settings = await service.LoadAsync();

        Assert.Equal(new HotkeyDefinition("T", Alt: true, Control: false, Shift: false, Windows: false),
            settings.TextTranslationHotkey);
        Assert.Equal("auto", settings.TextSourceLanguage);
        Assert.Equal("zh", settings.TextTargetLanguage);
        Assert.False(settings.FirstRunCompleted);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsSettings()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SettingsService(dir);
        var expected = AppSettings.CreateDefault() with
        {
            FirstRunCompleted = true,
            TextTargetLanguage = "en"
        };

        await service.SaveAsync(expected);
        var actual = await service.LoadAsync();

        Assert.Equal(expected, actual);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test --filter SettingsServiceTests
```

Expected: FAIL because the models/services do not exist.

- [ ] **Step 3: Implement settings models**

Create `src/LightTranslator/Models/HotkeyDefinition.cs`:

```csharp
namespace LightTranslator.Models;

public sealed record HotkeyDefinition(
    string Key,
    bool Alt,
    bool Control,
    bool Shift,
    bool Windows);
```

Create `src/LightTranslator/Models/LanguageOption.cs`:

```csharp
namespace LightTranslator.Models;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public static readonly IReadOnlyList<LanguageOption> SourceLanguages =
    [
        new("auto", "自动检测"),
        new("zh", "中文"),
        new("en", "English"),
        new("ja", "日本語")
    ];

    public static readonly IReadOnlyList<LanguageOption> TargetLanguages =
    [
        new("zh", "中文"),
        new("en", "English"),
        new("ja", "日本語")
    ];
}
```

Create `src/LightTranslator/Models/AppSettings.cs`:

```csharp
namespace LightTranslator.Models;

public sealed record AppSettings
{
    public bool FirstRunCompleted { get; init; }
    public string TextSourceLanguage { get; init; } = "auto";
    public string TextTargetLanguage { get; init; } = "zh";
    public string ScreenshotSourceLanguage { get; init; } = "auto";
    public string ScreenshotTargetLanguage { get; init; } = "zh";
    public HotkeyDefinition TextTranslationHotkey { get; init; } =
        new("T", Alt: true, Control: false, Shift: false, Windows: false);
    public HotkeyDefinition ScreenshotTranslationHotkey { get; init; } =
        new("Q", Alt: true, Control: false, Shift: false, Windows: false);
    public bool StartWithWindows { get; init; }

    public static AppSettings CreateDefault() => new();
}
```

- [ ] **Step 4: Implement JSON settings persistence**

Create `ISettingsService.cs`:

```csharp
using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
```

Create `SettingsService.cs` using `System.Text.Json`, with constructor `SettingsService(string? baseDirectory = null)`. When `baseDirectory` is null use:

```csharp
Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "LightTranslator");
```

Use indented JSON, create the directory before saving, and return `AppSettings.CreateDefault()` if `settings.json` does not exist.

- [ ] **Step 5: Run settings tests**

```powershell
dotnet test --filter SettingsServiceTests
```

Expected: PASS.

- [ ] **Step 6: Add the DPAPI package**

```powershell
dotnet add src/LightTranslator/LightTranslator.csproj package System.Security.Cryptography.ProtectedData
```

- [ ] **Step 7: Write failing DPAPI test**

Create `tests/LightTranslator.Tests/DpapiSecretStorageTests.cs`:

```csharp
using LightTranslator.Infrastructure.Security;

namespace LightTranslator.Tests;

public class DpapiSecretStorageTests
{
    [Fact]
    public void SaveLoadDelete_RoundTripsSecretWithoutPlaintextFile()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new DpapiSecretStorage(dir);

        storage.Save("deepseek-api-key", "secret-value");
        var loaded = storage.Load("deepseek-api-key");

        Assert.Equal("secret-value", loaded);

        var bytes = File.ReadAllBytes(Path.Combine(dir, "deepseek-api-key.bin"));
        Assert.DoesNotContain("secret-value", System.Text.Encoding.UTF8.GetString(bytes));

        storage.Delete("deepseek-api-key");
        Assert.Null(storage.Load("deepseek-api-key"));
    }
}
```

- [ ] **Step 8: Implement DPAPI storage**

Create `ISecretStorage.cs`:

```csharp
namespace LightTranslator.Infrastructure.Security;

public interface ISecretStorage
{
    void Save(string name, string secret);
    string? Load(string name);
    void Delete(string name);
}
```

Create `DpapiSecretStorage.cs`. Store each secret as `<name>.bin`. Encrypt/decrypt with:

```csharp
ProtectedData.Protect(
    plaintextBytes,
    optionalEntropy: null,
    DataProtectionScope.CurrentUser);
```

and:

```csharp
ProtectedData.Unprotect(
    encryptedBytes,
    optionalEntropy: null,
    DataProtectionScope.CurrentUser);
```

Reject blank secret names with `ArgumentException`.

- [ ] **Step 9: Run full tests**

```powershell
dotnet test
```

Expected: PASS.

- [ ] **Step 10: Commit**

```powershell
git add .
git commit -m "feat: add settings and secure secret storage"
```

---

### Task 3: Implement the DeepSeek translation boundary
**Files:**---

- Create: `src/LightTranslator/Models/TranslationRequest.cs`
- Create: `src/LightTranslator/Models/TranslationResult.cs`
- Create: `src/LightTranslator/Services/Translation/ITranslationService.cs`
- Create: `src/LightTranslator/Services/Translation/TranslationException.cs`
- Create: `src/LightTranslator/Services/Translation/DeepSeekTranslationService.cs`
- Test: `tests/LightTranslator.Tests/DeepSeekTranslationServiceTests.cs`
  **Interfaces:**

- Consumes:
  - `HttpClient`
  - API key supplied by `Func<string?>`
  - `TranslationRequest`
- Produces:
  - `Task<TranslationResult> TranslateAsync(
    TranslationRequest request,
    CancellationToken cancellationToken = default)`

- [ ] **Step 1: Write failing DeepSeek service tests**
  
  Create:
  `tests/LightTranslator.Tests/DeepSeekTranslationServiceTests.cs`
  The tests must use a fake `HttpMessageHandler`; they must not call the real DeepSeek API.
  Required behaviors:
  

```csharp
[Fact]
public async Task TranslateAsync_ParsesTranslationAndDetectedLanguage()
{
    var responseJson = """
    {
      "choices": [
        {
          "message": {
            "role": "assistant",
            "content": "{\"translated_text\":\"Hello\",\"detected_source_language\":\"zh\"}"
          }
        }
      ]
    }
    """;

    var handler = new StubHttpMessageHandler(
        HttpStatusCode.OK,
        responseJson);

    var service = new DeepSeekTranslationService(
        new HttpClient(handler),
        () => "test-api-key");

    var result = await service.TranslateAsync(
        new TranslationRequest(
            "你好",
            "auto",
            "en"));

    Assert.Equal("Hello", result.Text);
    Assert.Equal("zh", result.DetectedSourceLanguage);
}
```

```csharp
[Fact]
public async Task TranslateAsync_WhenApiKeyMissing_ThrowsConfigurationError()
{
    var service = new DeepSeekTranslationService(
        new HttpClient(
            new StubHttpMessageHandler(
                HttpStatusCode.OK,
                "{}")),
        () => null);

    var exception =
        await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                new TranslationRequest(
                    "你好",
                    "auto",
                    "en")));

    Assert.Equal(
        TranslationErrorKind.Configuration,
        exception.Kind);
}
```

```csharp
[Fact]
public async Task TranslateAsync_WhenUnauthorized_MapsToInvalidApiKey()
{
    var service = new DeepSeekTranslationService(
        new HttpClient(
            new StubHttpMessageHandler(
                HttpStatusCode.Unauthorized,
                "{}")),
        () => "invalid-key");

    var exception =
        await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                new TranslationRequest(
                    "你好",
                    "auto",
                    "en")));

    Assert.Equal(
        TranslationErrorKind.InvalidApiKey,
        exception.Kind);
}
```

```csharp
[Fact]
public async Task TranslateAsync_WhenBalanceIsInsufficient_MapsCorrectly()
{
    var service = new DeepSeekTranslationService(
        new HttpClient(
            new StubHttpMessageHandler(
                HttpStatusCode.PaymentRequired,
                "{}")),
        () => "test-key");

    var exception =
        await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                new TranslationRequest(
                    "你好",
                    "auto",
                    "en")));

    Assert.Equal(
        TranslationErrorKind.InsufficientBalance,
        exception.Kind);
}
```

```csharp
[Fact]
public async Task TranslateAsync_WhenRateLimited_MapsCorrectly()
{
    var service = new DeepSeekTranslationService(
        new HttpClient(
            new StubHttpMessageHandler(
                HttpStatusCode.TooManyRequests,
                "{}")),
        () => "test-key");

    var exception =
        await Assert.ThrowsAsync<TranslationException>(
            () => service.TranslateAsync(
                new TranslationRequest(
                    "你好",
                    "auto",
                    "en")));

    Assert.Equal(
        TranslationErrorKind.RateLimited,
        exception.Kind);
}
```

The test file defines:

```csharp
private sealed class StubHttpMessageHandler
    : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseJson;

    public StubHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseJson)
    {
        _statusCode = statusCode;
        _responseJson = responseJson;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseJson,
                    Encoding.UTF8,
                    "application/json")
            });
    }
}
```

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test --filter DeepSeekTranslationServiceTests
```

Expected:

```text
FAIL
```

because the translation contracts and service do not exist yet.

- [ ] **Step 3: Create translation request/result models**

`TranslationRequest.cs`:

```csharp
namespace LightTranslator.Models;

public sealed record TranslationRequest(
    string Text,
    string SourceLanguage,
    string TargetLanguage);
```

`TranslationResult.cs`:

```csharp
namespace LightTranslator.Models;

public sealed record TranslationResult(
    string Text,
    string? DetectedSourceLanguage);
```

- [ ] **Step 4: Create translation interface**

`ITranslationService.cs`:

```csharp
using LightTranslator.Models;

namespace LightTranslator.Services.Translation;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Create typed translation errors**

`TranslationException.cs`:

```csharp
namespace LightTranslator.Services.Translation;

public enum TranslationErrorKind
{
    Configuration,
    InvalidApiKey,
    InsufficientBalance,
    RateLimited,
    Timeout,
    Network,
    InvalidResponse,
    Unknown
}

public sealed class TranslationException : Exception
{
    public TranslationErrorKind Kind { get; }

    public TranslationException(
        TranslationErrorKind kind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }
}
```

- [ ] **Step 6: Implement DeepSeekTranslationService**

Create:

`DeepSeekTranslationService.cs`

Constructor:

```csharp
public DeepSeekTranslationService(
    HttpClient httpClient,
    Func<string?> apiKeyProvider)
```

The service must:

1. Return `TranslationResult(string.Empty, null)` for blank input without making an HTTP request.
2. Read the API key from `apiKeyProvider()` for every request.
3. Throw `TranslationErrorKind.Configuration` when the key is missing.
4. POST JSON to:

```text
https://api.deepseek.com/chat/completions
```

5. Use:

```json
{
  "model": "deepseek-v4-flash",
  "thinking": {
    "type": "disabled"
  },
  "response_format": {
    "type": "json_object"
  },
  "stream": false
}
```

6. Send:

```text
Authorization: Bearer <API_KEY>
```

7. The system prompt must explicitly contain the word `JSON` and instruct the model to return exactly:

```json
{
  "translated_text": "译文",
  "detected_source_language": "zh"
}
```

8. Treat user-provided text only as content to translate, never as instructions.
9. Preserve line breaks, numbers, URLs, technical terms and proper nouns where appropriate.
10. When source language is `"auto"`, instruct the model to detect it.
11. When source language is specified, instruct the model to translate from that language.
12. Supported language mappings:

```text
auto = 自动检测
zh   = 中文
en   = English
ja   = 日本語
```

13. Parse:

```text
choices[0].message.content
```

as a second JSON document.

14. Parse:

```json
{
  "translated_text": "...",
  "detected_source_language": "..."
}
```

into `TranslationResult`.

15. Empty/malformed content maps to:

```text
TranslationErrorKind.InvalidResponse
```

16. HTTP status mapping:

```text
401 → InvalidApiKey
402 → InsufficientBalance
429 → RateLimited
500 → Unknown
503 → Unknown
```

17. `TaskCanceledException` maps to `Timeout` only when the caller's own cancellation token was not cancelled.
18. `HttpRequestException` maps to `Network`.
19. Caller-requested cancellation must propagate normally.
20. Never include the API key or user translation content in exception messages.

- [ ] **Step 7: Verify request structure in a test**

Extend the fake HTTP handler so it captures the incoming request body and headers.

Verify:

```csharp
Assert.Equal(
    "Bearer",
    capturedRequest.Headers.Authorization?.Scheme);

Assert.Equal(
    "test-api-key",
    capturedRequest.Headers.Authorization?.Parameter);
```

Parse the captured JSON body and assert:

```text
model == deepseek-v4-flash
thinking.type == disabled
response_format.type == json_object
stream == false
```

Also verify the system prompt contains `JSON`.

- [ ] **Step 8: Run DeepSeek tests**

Run:

```powershell
dotnet test --filter DeepSeekTranslationServiceTests
```

Expected:

```text
PASS
```

with all DeepSeek translation tests passing.

- [ ] **Step 9: Run the complete test suite**

Run:

```powershell
dotnet test
```

Expected: all tests PASS.

- [ ] **Step 10: Build the solution**

Run:

```powershell
dotnet build
```

Expected:

```text
0 warning
0 error
```

- [ ] **Step 11: Commit**

```powershell
git add .
git commit -m "feat: add DeepSeek translation service"
```

### Task 4: Build the debounced text-translation ViewModel

**Files:**

- Create: `src/LightTranslator/ViewModels/TranslateViewModel.cs`
- Test: `tests/LightTranslator.Tests/TranslateViewModelTests.cs`

**Interfaces:**

- Consumes:

  - `ITranslationService.TranslateAsync(...)`
  - current source/target language codes.

- Produces:

  - bindable `InputText`, `TranslatedText`, `IsTranslating`, `ErrorMessage`,
    `SourceLanguage`, `TargetLanguage`, `DetectedSourceLanguage`.
  - `Task FlushAsync()` for deterministic tests.
  - `void SwapLanguages()`.

- [ ] **Step 1: Write failing debounce tests**

Create tests that prove three behaviors:

```csharp
[Fact]
public async Task InputChange_TranslatesAfterDebounce()
{
    var fake = new FakeTranslationService("Hello", "zh");
    var vm = new TranslateViewModel(fake, TimeSpan.FromMilliseconds(10))
    {
        SourceLanguage = "auto",
        TargetLanguage = "en",
        InputText = "你好"
    };

    await vm.FlushAsync();

    Assert.Equal("Hello", vm.TranslatedText);
    Assert.Equal(1, fake.CallCount);
}

[Fact]
public async Task RapidInput_OnlyLatestRequestWins()
{
    var fake = new ControllableTranslationService();
    var vm = new TranslateViewModel(fake, TimeSpan.Zero)
    {
        TargetLanguage = "en"
    };

    vm.InputText = "first";
    vm.InputText = "second";

    fake.CompleteLatest(new TranslationResult("SECOND", "zh"));
    await vm.FlushAsync();

    Assert.Equal("SECOND", vm.TranslatedText);
}

[Fact]
public void SwapLanguages_WhenSourceIsAuto_UsesDetectedSource()
{
    var fake = new FakeTranslationService("Hello", "zh");
    var vm = new TranslateViewModel(fake, TimeSpan.Zero)
    {
        SourceLanguage = "auto",
        TargetLanguage = "en"
    };
    vm.SetDetectedSourceLanguageForTest("zh");

    vm.SwapLanguages();

    Assert.Equal("en", vm.SourceLanguage);
    Assert.Equal("zh", vm.TargetLanguage);
}
```

The fake service classes live in the same test file.

- [ ] **Step 2: Run test and verify failure**

```powershell
dotnet test --filter TranslateViewModelTests
```

Expected: FAIL.

- [ ] **Step 3: Implement `INotifyPropertyChanged` ViewModel**

Requirements:

- `InputText` setter cancels any pending debounce/translation token.
- Blank/whitespace input immediately clears output and error.
- Translation scheduling uses:

```csharp
await Task.Delay(_debounceDelay, token);
```

- Translation runs asynchronously with a `CancellationTokenSource`.

- Only the currently active request may update `TranslatedText`.

- `TranslationException` maps to these user-facing strings:

  - `Configuration` -> `请先配置 DeepSeek API Key`
  - `InvalidApiKey` -> `DeepSeek API Key 无效`
  - `QuotaExceeded` -> `DeepSeek API 额度不足`
  - `Timeout` -> `翻译请求超时`
  - `Network` -> `网络连接失败`
  - default -> `翻译失败`

- Never include source text in `ErrorMessage`.

- `FlushAsync()` awaits the current scheduled translation task.

- `SwapLanguages()`:

  - if source is `auto`, use `DetectedSourceLanguage`; if null, do nothing.
  - swap source and target.
  - if `InputText` is nonblank, schedule a new translation.

- [ ] **Step 4: Run tests**

```powershell
dotnet test --filter TranslateViewModelTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add .
git commit -m "feat: add debounced translation view model"
```

---

### Task 5: Build the TranslateWindow interaction

**Files:**

- Create: `src/LightTranslator/Services/Clipboard/IClipboardService.cs`
- Create: `src/LightTranslator/Services/Clipboard/ClipboardService.cs`
- Modify: `src/LightTranslator/Views/TranslateWindow.xaml`
- Modify: `src/LightTranslator/Views/TranslateWindow.xaml.cs`

**Interfaces:**

- Consumes: `TranslateViewModel`, `IClipboardService`.

- Produces: borderless translation window with keyboard behavior required by the spec.

- [ ] **Step 1: Add clipboard abstraction**

`IClipboardService.cs`:

```csharp
namespace LightTranslator.Services.Clipboard;

public interface IClipboardService
{
    void SetText(string text);
}
```

`ClipboardService.cs`:

```csharp
namespace LightTranslator.Services.Clipboard;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text) => System.Windows.Clipboard.SetText(text);
}
```

- [ ] **Step 2: Replace generated MainWindow with TranslateWindow**

Delete the generated `MainWindow.xaml` / `.xaml.cs`.

Create a borderless `TranslateWindow` with:

```xml
<Window
    x:Class="LightTranslator.Views.TranslateWindow"
    ...
    Width="520"
    Height="320"
    WindowStyle="None"
    ResizeMode="NoResize"
    ShowInTaskbar="False"
    WindowStartupLocation="Manual"
    Background="Transparent"
    AllowsTransparency="True">
```

Inside use one rounded `Border` containing:

- top row: source `ComboBox`, swap `Button`, target `ComboBox`
- middle: editable multiline input `TextBox`
- divider
- bottom: read-only translated `TextBox`
- small copy button
- small status text bound to `ErrorMessage` / `IsTranslating`

Do not add history, microphone, favorites, or extra toolbar buttons.

- [ ] **Step 3: Bind language lists and state**

Set `DataContext` to the injected `TranslateViewModel`.

Source ComboBox uses `LanguageOption.SourceLanguages`.
Target ComboBox uses `LanguageOption.TargetLanguages`.

Bind selected code to `SourceLanguage` / `TargetLanguage`.

- [ ] **Step 4: Implement keyboard behavior in code-behind**

The input TextBox `PreviewKeyDown` must behave:

```csharp
if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
{
    // Let the TextBox insert a newline.
    return;
}

if (e.Key == Key.Enter)
{
    if (!string.IsNullOrWhiteSpace(ViewModel.TranslatedText))
    {
        _clipboard.SetText(ViewModel.TranslatedText);
        Close();
    }

    e.Handled = true;
}
else if (e.Key == Key.Escape)
{
    Close();
    e.Handled = true;
}
```

Window-level `Escape` also closes if focus is elsewhere.

Copy button calls `_clipboard.SetText(ViewModel.TranslatedText)` and leaves the window open.

- [ ] **Step 5: Focus input on open**

On `Loaded`:

```csharp
InputTextBox.Focus();
Keyboard.Focus(InputTextBox);
```

- [ ] **Step 6: Position on the monitor containing the cursor**

For Phase 1, use `System.Windows.Forms.Screen.FromPoint(Cursor.Position)` and convert screen pixels to WPF DIP using the window DPI after source initialization. Place the 520×320 window centered horizontally and around 35% from the top of the working area.

- [ ] **Step 7: Manual interaction test**

Run:

```powershell
dotnet run --project src/LightTranslator/LightTranslator.csproj
```

Temporarily instantiate `TranslateWindow` from `App.OnStartup` with a fake translation service.

Verify:

- input autofocuses
- Shift+Enter inserts newline
- Enter copies + closes only if translation exists
- Escape closes
- copy button does not close
- source/target dropdowns display Auto/ZH/EN/JA correctly

Remove the temporary startup code before commit.

- [ ] **Step 8: Commit**

```powershell
git add .
git commit -m "feat: add text translation window"
```

---

### Task 6: Implement testable global hotkeys and WindowManager

**Files:**

- Create: `src/LightTranslator/Services/Hotkeys/HotkeyAction.cs`
- Create: `src/LightTranslator/Services/Hotkeys/IHotkeyBackend.cs`
- Create: `src/LightTranslator/Services/Hotkeys/NativeHotkeyBackend.cs`
- Create: `src/LightTranslator/Services/Hotkeys/HotkeyService.cs`
- Create: `src/LightTranslator/Infrastructure/Windows/NativeMethods.cs`
- Create: `src/LightTranslator/Services/Windows/WindowManager.cs`
- Test: `tests/LightTranslator.Tests/HotkeyServiceTests.cs`

**Interfaces:**

- Consumes: `HotkeyDefinition`, a WPF HWND.

- Produces:

  - `bool HotkeyService.TryRegister(HotkeyAction action, HotkeyDefinition hotkey)`
  - `void UnregisterAll()`
  - event `Action<HotkeyAction>? Triggered`
  - `WindowManager.ToggleTranslateWindow()`

- [ ] **Step 1: Write failing HotkeyService tests**

Use an in-memory fake backend:

```csharp
[Fact]
public void TryRegister_WhenBackendRejects_DoesNotReplaceExistingBinding()
{
    var backend = new FakeHotkeyBackend();
    var service = new HotkeyService(backend);

    backend.NextRegisterResult = true;
    Assert.True(service.TryRegister(
        HotkeyAction.TextTranslation,
        new HotkeyDefinition("T", true, false, false, false)));

    backend.NextRegisterResult = false;
    Assert.False(service.TryRegister(
        HotkeyAction.TextTranslation,
        new HotkeyDefinition("Y", true, false, false, false)));

    Assert.Equal("T", service.GetRegistered(HotkeyAction.TextTranslation)!.Key);
}

[Fact]
public void BackendTrigger_RaisesTypedAction()
{
    var backend = new FakeHotkeyBackend();
    var service = new HotkeyService(backend);
    service.TryRegister(
        HotkeyAction.TextTranslation,
        new HotkeyDefinition("T", true, false, false, false));

    HotkeyAction? received = null;
    service.Triggered += action => received = action;

    backend.Raise(service.GetRegistrationId(HotkeyAction.TextTranslation));

    Assert.Equal(HotkeyAction.TextTranslation, received);
}
```

- [ ] **Step 2: Implement hotkey action and backend abstraction**

`HotkeyAction.cs`:

```csharp
namespace LightTranslator.Services.Hotkeys;

public enum HotkeyAction
{
    TextTranslation,
    ScreenshotTranslation
}
```

`IHotkeyBackend.cs`:

```csharp
namespace LightTranslator.Services.Hotkeys;

public interface IHotkeyBackend : IDisposable
{
    event Action<int>? HotkeyPressed;
    bool Register(int id, Models.HotkeyDefinition hotkey);
    void Unregister(int id);
}
```

- [ ] **Step 3: Implement Win32 RegisterHotKey backend**

`NativeMethods.cs` defines:

```csharp
[DllImport("user32.dll")]
internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

[DllImport("user32.dll")]
internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
```

`NativeHotkeyBackend` accepts the application message-window HWND and converts:

- Alt -> `MOD_ALT`
- Control -> `MOD_CONTROL`
- Shift -> `MOD_SHIFT`
- Windows -> `MOD_WIN`
- key string -> `KeyInterop.VirtualKeyFromKey(...)`

It listens for `WM_HOTKEY` through an `HwndSourceHook` and raises `HotkeyPressed(id)`.

- [ ] **Step 4: Implement HotkeyService registration replacement semantics**

Use stable IDs:

```csharp
TextTranslation = 1001
ScreenshotTranslation = 1002
```

When changing an existing hotkey:

1. keep the previous definition in memory.
2. unregister old.
3. try new.
4. if new fails, re-register old.
5. return false.
6. only update stored definition when new registration succeeds.
- [ ] **Step 5: Run hotkey tests**

```powershell
dotnet test --filter HotkeyServiceTests
```

Expected: PASS.

- [ ] **Step 6: Implement WindowManager**

Constructor dependencies:

```csharp
Func<TranslateWindow> translateWindowFactory
```

`ToggleTranslateWindow()` behavior:

- if current window exists and `IsVisible`, close it and clear reference.

- otherwise create a new TranslateWindow, subscribe `Closed` to clear reference, and show it.

- only one TranslateWindow may exist.

- [ ] **Step 7: Commit**

```powershell
git add .
git commit -m "feat: add global hotkeys and window manager"
```

---

### Task 7: Implement first-run settings, tray lifecycle, and AppController

**Files:**

- Create: `src/LightTranslator/ViewModels/SettingsViewModel.cs`
- Create: `src/LightTranslator/Views/WelcomeWindow.xaml`
- Create: `src/LightTranslator/Views/WelcomeWindow.xaml.cs`
- Create: `src/LightTranslator/Views/SettingsWindow.xaml`
- Create: `src/LightTranslator/Views/SettingsWindow.xaml.cs`
- Create: `src/LightTranslator/Services/Tray/TrayService.cs`
- Create: `src/LightTranslator/AppController.cs`
- Modify: `src/LightTranslator/App.xaml`
- Modify: `src/LightTranslator/App.xaml.cs`

**Interfaces:**

- Consumes: settings, secret storage, hotkeys, translation service, `WindowManager`.

- Produces: first-run onboarding; subsequent silent tray startup; tray commands.

- [ ] **Step 1: Remove StartupUri**

`App.xaml` must not declare `StartupUri`.

Use:

```xml
<Application
    x:Class="LightTranslator.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ShutdownMode="OnExplicitShutdown">
</Application>
```

- [ ] **Step 2: Implement SettingsViewModel validation**

Expose:

```csharp
public string DeepSeekApiKey { get; set; }
public string TextSourceLanguage { get; set; }
public string TextTargetLanguage { get; set; }
public HotkeyDefinition TextTranslationHotkey { get; set; }
public string? StatusMessage { get; }
public bool IsBusy { get; }
```

Add:

```csharp
Task<bool> TestDeepSeekAsync(CancellationToken cancellationToken = default)
Task<bool> SaveAsync(CancellationToken cancellationToken = default)
```

`TestDeepSeekAsync` temporarily uses the entered key and translates the fixed internal probe text `"Hello"` to Chinese. Do not log the probe response.

`SaveAsync` must:

1. validate key nonblank.
2. attempt hotkey registration before settings persistence.
3. if hotkey registration fails, set `StatusMessage = "快捷键已被其他程序占用"` and return false.
4. DPAPI-save key only after validation succeeds.
5. save settings with `FirstRunCompleted = true`.
- [ ] **Step 3: Build WelcomeWindow**

Keep it one page:

- title/logo text
- DeepSeek API Key password box
- source/target defaults
- text hotkey display/editor
- “测试连接” button
- “完成” button

On success, close the window and let `AppController` continue into tray mode.

- [ ] **Step 4: Build SettingsWindow**

Reuse the same settings ViewModel and controls, but label the main action “保存”.

V1 screenshot-language settings may be visible and persisted, but `Alt+Q` should show a short notification such as “截图翻译将在下一阶段启用” rather than starting capture.

- [ ] **Step 5: Implement TrayService**

Use `System.Windows.Forms.NotifyIcon`.

Menu:

```text
文本翻译        Alt + T
截图翻译        Alt + Q
----------------------
设置
----------------------
退出
```

Expose events:

```csharp
event Action? TextTranslationRequested;
event Action? ScreenshotTranslationRequested;
event Action? SettingsRequested;
event Action? ExitRequested;
```

Dispose the icon on application shutdown so no ghost icon remains.

- [ ] **Step 6: Implement AppController**

`StartAsync()`:

1. load settings.
2. create a hidden message-only WPF `Window` or `HwndSource` required by `NativeHotkeyBackend`.
3. construct `HotkeyService`.
4. if first run is incomplete:
   - show `WelcomeWindow` modally.
   - if user closes before successful completion, shut down application.
   - reload saved settings.
5. register text hotkey.
6. register screenshot hotkey as reserved action.
7. create TrayService.
8. wire tray + hotkeys to the same command methods.
9. remain running with no visible main window.

Handlers:

```csharp
private void ToggleTextTranslation()
    => _windowManager.ToggleTranslateWindow();

private void HandleScreenshotTranslation()
    => _notificationService.Show("截图翻译将在下一阶段启用");
```

For Phase 1, the screenshot action must not throw or silently do nothing.

- [ ] **Step 7: Wire App startup and shutdown**

`App.xaml.cs`:

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    _controller = AppController.CreateDefault();
    await _controller.StartAsync();
}

protected override void OnExit(ExitEventArgs e)
{
    _controller?.Dispose();
    base.OnExit(e);
}
```

If startup throws, show a short message box without secrets/user content and exit.

- [ ] **Step 8: Manual first-run test**

Delete:

```text
%LocalAppData%\LightTranslator
```

Run:

```powershell
dotnet run --project src/LightTranslator/LightTranslator.csproj
```

Verify:

1. WelcomeWindow appears.
2. Closing onboarding exits the process.
3. invalid key test gives a clear message.
4. valid key can be saved.
5. app enters tray with no main window.
6. exiting tray removes process and tray icon.
- [ ] **Step 9: Manual second-run test**

Start again.

Verify:

- no WelcomeWindow

- tray appears

- Alt+T opens window

- Alt+T again closes it

- tray “文本翻译” uses the same behavior

- Settings opens

- Alt+Q produces the Phase 2 informational notification

- [ ] **Step 10: Commit**

```powershell
git add .
git commit -m "feat: add first-run and tray application lifecycle"
```

---

### Task 8: Persist text-language choices and complete keyboard workflow

**Files:**

- Modify: `src/LightTranslator/Services/Windows/WindowManager.cs`
- Modify: `src/LightTranslator/ViewModels/TranslateViewModel.cs`
- Modify: `src/LightTranslator/Views/TranslateWindow.xaml.cs`
- Modify: `src/LightTranslator/AppController.cs`
- Test: `tests/LightTranslator.Tests/TranslateViewModelTests.cs`

**Interfaces:**

- Consumes: current `AppSettings`.

- Produces: last-used text source/target language restored on next open/restart.

- [ ] **Step 1: Add failing language-state test**

Add:

```csharp
[Fact]
public void LanguageChanges_RaiseLanguagePreferenceChanged()
{
    var vm = new TranslateViewModel(
        new FakeTranslationService("x", "zh"),
        TimeSpan.Zero);

    (string Source, string Target)? changed = null;
    vm.LanguagePreferenceChanged += (source, target) =>
        changed = (source, target);

    vm.SourceLanguage = "zh";
    vm.TargetLanguage = "en";

    Assert.Equal(("zh", "en"), changed);
}
```

- [ ] **Step 2: Implement language preference event**

Add:

```csharp
public event Action<string, string>? LanguagePreferenceChanged;
```

Raise it after valid source/target changes. Never raise when the source/target value is unchanged.

- [ ] **Step 3: Persist preferences through AppController**

When creating each `TranslateViewModel`:

- initialize languages from current settings.
- subscribe to `LanguagePreferenceChanged`.
- update in-memory settings.
- persist using `SettingsService.SaveAsync`.

Use a serialized save gate (`SemaphoreSlim(1,1)`) so rapid dropdown changes cannot corrupt `settings.json`.

- [ ] **Step 4: Verify Enter behavior with the real application**

Manual test:

1. Alt+T.
2. enter Chinese text.
3. wait for DeepSeek result.
4. press Shift+Enter during input and confirm newline.
5. press Enter after result.
6. paste into Notepad and verify translated text was copied.
7. confirm translator window closed.
8. reopen Alt+T; language pair is preserved.
- [ ] **Step 5: Run all automated tests**

```powershell
dotnet test
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat: persist text translation preferences"
```

---

### Task 9: Apply the Start-with-Windows preference

**Files:**

- Create: `src/LightTranslator/Services/Startup/IStartupService.cs`
- Create: `src/LightTranslator/Services/Startup/StartupService.cs`
- Modify: `src/LightTranslator/ViewModels/SettingsViewModel.cs`
- Modify: `src/LightTranslator/AppController.cs`
- Test: `tests/LightTranslator.Tests/StartupServiceTests.cs`

**Interfaces:**

- Consumes: executable path and `AppSettings.StartWithWindows`.

- Produces:

  - `bool IStartupService.IsEnabled()`
  - `void SetEnabled(bool enabled)`

- [ ] **Step 1: Create the interface**

```csharp
namespace LightTranslator.Services.Startup;

public interface IStartupService
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}
```

- [ ] **Step 2: Write a failing test against an abstract registry backend**

In `StartupServiceTests.cs`, use an in-memory backend and verify that enabling writes exactly one value named `LightTranslator`, while disabling removes it:

```csharp
[Fact]
public void SetEnabled_TrueThenFalse_WritesAndRemovesRunEntry()
{
    var backend = new FakeStartupBackend();
    var service = new StartupService(
        backend,
        () => @"C:\Apps\LightTranslator\LightTranslator.exe");

    service.SetEnabled(true);

    Assert.Equal(
        "\"C:\\Apps\\LightTranslator\\LightTranslator.exe\"",
        backend.Values["LightTranslator"]);

    service.SetEnabled(false);

    Assert.False(backend.Values.ContainsKey("LightTranslator"));
}
```

- [ ] **Step 3: Implement the registry backend and service**

Use the current-user Run key only:

```csharp
const string RunKey =
    @"Software\Microsoft\Windows\CurrentVersion\Run";
const string ValueName = "LightTranslator";
```

Enable value:

```csharp
var exe = _executablePathProvider();
var command = $"\"{exe}\"";
_registry.SetValue(RunKey, ValueName, command);
```

Disable:

```csharp
_registry.DeleteValue(RunKey, ValueName);
```

Do not request administrator rights and do not write HKLM.

- [ ] **Step 4: Wire the setting**

`SettingsViewModel.SaveAsync()` must call:

```csharp
_startupService.SetEnabled(StartWithWindows);
```

only after the rest of settings validation succeeds.

On load, initialize the checkbox from persisted settings and reconcile it with `IStartupService.IsEnabled()`; the actual registry state wins.

- [ ] **Step 5: Run tests**

```powershell
dotnet test --filter StartupServiceTests
```

Expected: PASS.

- [ ] **Step 6: Manual verification**

Enable “开机启动” in Settings and verify this value exists:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
LightTranslator = "<full path>\LightTranslator.exe"
```

Disable it and verify the value is removed.

- [ ] **Step 7: Commit**

```powershell
git add .
git commit -m "feat: add start with Windows preference"
```

---

### Task 10: Add privacy-safe logging and startup diagnostics

**Files:**

- Create: `src/LightTranslator/Services/Logging/AppLogger.cs`
- Modify: `src/LightTranslator/AppController.cs`
- Modify: `src/LightTranslator/Services/Translation/DeepSeekTranslationService.cs`
- Modify: `src/LightTranslator/ViewModels/TranslateViewModel.cs`
- Test: `tests/LightTranslator.Tests/AppLoggerTests.cs`

**Interfaces:**

- Consumes: log event name, safe metadata.

- Produces: daily file under `%LocalAppData%\LightTranslator\logs`.

- [ ] **Step 1: Write failing privacy test**

Create `AppLoggerTests.cs`:

```csharp
[Fact]
public void Log_WritesEventAndMetadata()
{
    var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var logger = new AppLogger(dir);

    logger.Info("translation.completed", ("elapsed_ms", 123));

    var file = Directory.GetFiles(dir, "*.log").Single();
    var text = File.ReadAllText(file);

    Assert.Contains("translation.completed", text);
    Assert.Contains("elapsed_ms=123", text);
}
```

Do not design a logger API that accepts arbitrary exception context dictionaries containing request objects.

- [ ] **Step 2: Implement minimal daily logger**

Format:

```text
2026-09-13T12:34:56.123+08:00 INFO translation.completed elapsed_ms=123
```

Expose only:

```csharp
void Info(string eventName, params (string Key, object? Value)[] metadata);
void Error(string eventName, Exception exception, params (string Key, object? Value)[] metadata);
```

Sanitize line breaks in metadata. Never log exception message for `TranslationException` if it could originate from a remote API response; log exception type and `TranslationErrorKind` instead.

- [ ] **Step 3: Add safe events**

Log:

- `app.start`
- `app.exit`
- `hotkey.registered` with action only
- `hotkey.registration_failed` with action only
- `translation.started`
- `translation.completed` with elapsed milliseconds
- `translation.failed` with error kind
- `settings.saved`

Do not log:

- input text

- translated text

- detected content

- API key

- [ ] **Step 4: Run tests**

```powershell
dotnet test
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add .
git commit -m "feat: add privacy-safe application logging"
```

---

### Task 11: Phase 1 acceptance verification and publish smoke test

**Files:**

- Modify only if verification exposes defects.
- Create: `docs/phase1-acceptance.md`

**Interfaces:**

- Consumes: completed Phase 1 application.

- Produces: verified releasable Phase 1 build and acceptance record.

- [ ] **Step 1: Run clean automated test suite**

```powershell
dotnet clean
dotnet test
```

Expected: all tests PASS, no failed tests.

- [ ] **Step 2: Run Release build**

```powershell
dotnet build -c Release
```

Expected: `0 Error(s)`.

- [ ] **Step 3: Publish a self-contained Windows x64 build**

Run:

```powershell
dotnet publish src/LightTranslator/LightTranslator.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

Expected: publish directory contains `LightTranslator.exe`.

Do not enable trimming in Phase 1; WPF reflection/resource behavior makes trimming an unnecessary risk at this stage.

- [ ] **Step 4: Perform acceptance checklist**

Create `docs/phase1-acceptance.md` and record PASS/FAIL for each:

```text
[ ] First launch shows WelcomeWindow.
[ ] API key is not present in settings.json.
[ ] API key is not readable as plaintext in its secret file.
[ ] Subsequent launch goes directly to tray.
[ ] Tray exit fully terminates process.
[ ] Alt+T opens translator on current monitor.
[ ] Alt+T while open closes translator.
[ ] Input autofocus works.
[ ] Translation starts after roughly 400ms idle.
[ ] Rapid typing does not allow stale result overwrite.
[ ] Enter copies result and closes.
[ ] Shift+Enter inserts newline.
[ ] Escape closes without copying.
[ ] Copy button copies without closing.
[ ] Text source/target language persists across restarts.
[ ] Invalid API key produces a clear error.
[ ] Network failure produces a clear error.
[ ] Logs contain no translation text or API key.
[ ] Enabling Start with Windows creates only the current-user Run entry.
[ ] Disabling Start with Windows removes that entry.
[ ] Alt+Q does not crash; Phase 2 notice appears.
```

- [ ] **Step 5: Verify tray/hotkey cleanup after abnormal close**

Start application, then trigger a normal tray Exit. Restart immediately.

Expected: default hotkeys register again without requiring reboot or sign-out.

- [ ] **Step 6: Final commit**

```powershell
git add .
git commit -m "test: verify LightTranslator phase 1"
```

---

## Phase 1 Definition of Done

Phase 1 is complete only when:

1. `dotnet test` passes.
2. clean Release build succeeds.
3. first-run configuration works.
4. API key is DPAPI-protected.
5. application normally lives only in the tray.
6. `Alt + T` completes the full text translation workflow.
7. language preferences persist.
8. stale async translation results cannot overwrite newer ones.
9. privacy-safe logging is active.
10. Start with Windows can be enabled/disabled without administrator rights.
11. a self-contained Windows x64 build launches successfully.

At that point the application is already useful as a lightweight text translator. Phase 2 should then implement `Alt + Q`: monitor-aware frozen capture, local PaddleOCR ONNX, `OcrBlock`, DeepSeek block translation, `BackgroundCleaner`, `TranslationRenderer`, Loading Overlay, and final `OverlayWindow`.

---

## Plan Self-Review

- **Spec coverage:** Phase 1 intentionally covers the complete application foundation and text-translation subsystem. Screenshot capture/OCR/rendering requirements are isolated to Phase 2 rather than partially implemented here.
- **Placeholder scan:** no `TBD`, `TODO`, or unspecified implementation placeholders remain.
- **Type consistency:** production app targets `net8.0-windows`; the test project also targets `net8.0-windows` so it can reference the WPF project.
- **Privacy check:** no test or runtime requirement permits logging translation content, OCR content, screenshots, or API keys.
- **Scope check:** Phase 1 ends in independently usable software; Phase 2 can be planned and reviewed separately without changing the Phase 1 public interfaces.
