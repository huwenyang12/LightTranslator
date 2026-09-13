using LightTranslator.Models;
using LightTranslator.Services.Translation;
using LightTranslator.ViewModels;

namespace LightTranslator.Tests;

public class TranslateViewModelTests
{
    [Fact]
    public async Task SourceTextChanged_AfterDebounce_TranslatesText()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => viewModel.TranslatedText == "Hello"
        );

        Assert.Equal(
            "你好",
            translationService.LastRequest?.Text
        );

        Assert.Equal(
            "auto",
            translationService.LastRequest?.SourceLanguage
        );

        Assert.Equal(
            "en",
            translationService.LastRequest?.TargetLanguage
        );

        Assert.Equal(
            "Hello",
            viewModel.TranslatedText
        );
    }

    [Fact]
    public async Task SourceTextChanged_Quickly_OnlyTranslatesLatestText()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(30)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";

        viewModel.SourceText = "你";
        viewModel.SourceText = "你好";
        viewModel.SourceText = "你好啊";

        await Task.Delay(100);

        Assert.Equal(
            1,
            translationService.CallCount
        );

        Assert.Equal(
            "你好啊",
            translationService.LastRequest?.Text
        );
    }

    [Fact]
    public async Task TranslationConfigurationError_ShowsUserFriendlyMessage()
    {
        var translationService =
            new ThrowingTranslationService(
                new TranslationException(
                    TranslationErrorKind.Configuration,
                    "Internal configuration error."
                )
            );

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => viewModel.ErrorMessage is not null
        );

        Assert.Equal(
            "请先配置 DeepSeek API Key",
            viewModel.ErrorMessage
        );
    }

    [Fact]
    public async Task SuccessfulTranslation_ClearsPreviousErrorMessage()
    {
        var translationService =
            new RecoverableTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";

        viewModel.SourceText = "第一次";

        await WaitUntilAsync(
            () => viewModel.ErrorMessage is not null
        );

        Assert.Equal(
            "网络连接失败",
            viewModel.ErrorMessage
        );

        viewModel.SourceText = "第二次";

        await WaitUntilAsync(
            () => viewModel.TranslatedText == "Success"
        );

        Assert.Null(
            viewModel.ErrorMessage
        );
    }

    [Fact]
    public void SwapLanguages_WhenSourceIsExplicit_SwapsSourceAndTarget()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "zh";
        viewModel.TargetLanguage = "en";

        viewModel.SwapLanguages();

        Assert.Equal(
            "en",
            viewModel.SourceLanguage
        );

        Assert.Equal(
            "zh",
            viewModel.TargetLanguage
        );
    }

    [Fact]
    public async Task SwapLanguages_WhenSourceIsAuto_UsesDetectedSourceLanguage()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => viewModel.TranslatedText == "Hello"
        );

        viewModel.SwapLanguages();

        Assert.Equal(
            "en",
            viewModel.SourceLanguage
        );

        Assert.Equal(
            "zh",
            viewModel.TargetLanguage
        );
    }


    [Fact]
    public void Constructor_UsesInitialLanguageSelection()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel =
            new TranslateViewModel(
                translationService,
                initialSourceLanguage: "en",
                initialTargetLanguage: "ja"
            );

        Assert.Equal(
            "en",
            viewModel.SourceLanguage
        );

        Assert.Equal(
            "ja",
            viewModel.TargetLanguage
        );
    }

    [Fact]
    public async Task SourceLanguageChanged_RetranslatesCurrentText()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => translationService.CallCount == 1
        );

        viewModel.SourceLanguage = "zh";

        await WaitUntilAsync(
            () => translationService.CallCount == 2
        );

        Assert.Equal(
            "你好",
            translationService.LastRequest?.Text
        );

        Assert.Equal(
            "zh",
            translationService.LastRequest?.SourceLanguage
        );

        Assert.Equal(
            "en",
            translationService.LastRequest?.TargetLanguage
        );
    }

    [Theory]
    [InlineData(
        TranslationErrorKind.InvalidApiKey,
        "DeepSeek API Key 无效"
    )]
    [InlineData(
        TranslationErrorKind.InsufficientBalance,
        "DeepSeek API 余额不足"
    )]
    [InlineData(
        TranslationErrorKind.RateLimited,
        "请求过于频繁，请稍后重试"
    )]
    [InlineData(
        TranslationErrorKind.Timeout,
        "翻译请求超时"
    )]
    [InlineData(
        TranslationErrorKind.Network,
        "网络连接失败"
    )]
    [InlineData(
        TranslationErrorKind.InvalidResponse,
        "翻译结果解析失败"
    )]
    [InlineData(
        TranslationErrorKind.Unknown,
        "翻译失败"
    )]
    public async Task TranslationError_ShowsUserFriendlyMessage(
        TranslationErrorKind kind,
        string expectedMessage
    )
    {
        var translationService =
            new ThrowingTranslationService(
                new TranslationException(
                    kind,
                    "Internal translation error."
                )
            );

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => viewModel.ErrorMessage is not null
        );

        Assert.Equal(
            expectedMessage,
            viewModel.ErrorMessage
        );
    }

    [Fact]
    public async Task TargetLanguageChanged_RetranslatesCurrentText()
    {
        var translationService =
            new FakeTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.FromMilliseconds(10)
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";
        viewModel.SourceText = "你好";

        await WaitUntilAsync(
            () => translationService.CallCount == 1
        );

        viewModel.TargetLanguage = "ja";

        await WaitUntilAsync(
            () => translationService.CallCount == 2
        );

        Assert.Equal(
            "你好",
            translationService.LastRequest?.Text
        );

        Assert.Equal(
            "auto",
            translationService.LastRequest?.SourceLanguage
        );

        Assert.Equal(
            "ja",
            translationService.LastRequest?.TargetLanguage
        );
    }

    [Fact]
    public async Task OlderRequest_WhenCompletedLater_DoesNotOverwriteLatestResult()
    {
        var translationService =
            new ControlledTranslationService();

        var viewModel = new TranslateViewModel(
            translationService,
            TimeSpan.Zero
        );

        viewModel.SourceLanguage = "auto";
        viewModel.TargetLanguage = "en";

        viewModel.SourceText = "旧文本";

        await WaitUntilAsync(
            () => translationService.CallCount == 1
        );

        viewModel.SourceText = "新文本";

        await WaitUntilAsync(
            () => translationService.CallCount == 2
        );

        translationService.Complete(
            "新文本",
            "NEW RESULT"
        );

        await WaitUntilAsync(
            () => viewModel.TranslatedText == "NEW RESULT"
        );

        translationService.Complete(
            "旧文本",
            "OLD RESULT"
        );

        await Task.Delay(50);

        Assert.Equal(
            "NEW RESULT",
            viewModel.TranslatedText
        );
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        int timeoutMilliseconds = 1000
    )
    {
        var start = Environment.TickCount64;

        while (!condition())
        {
            if (
                Environment.TickCount64 - start
                > timeoutMilliseconds
            )
            {
                throw new TimeoutException(
                    "Condition was not met in time."
                );
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeTranslationService
        : ITranslationService
    {
        private int _callCount;

        public int CallCount =>
            Volatile.Read(ref _callCount);

        public TranslationRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            Interlocked.Increment(
                ref _callCount
            );

            LastRequest = request;

            return Task.FromResult(
                new TranslationResult(
                    "Hello",
                    "zh"
                )
            );
        }
    }

    private sealed class ControlledTranslationService
        : ITranslationService
    {
        private readonly object _lock = new();

        private readonly Dictionary<
            string,
            TaskCompletionSource<TranslationResult>
        > _pending = new();

        private int _callCount;

        public int CallCount =>
            Volatile.Read(ref _callCount);

        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var completionSource =
                new TaskCompletionSource<TranslationResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously
                );

            lock (_lock)
            {
                _pending[request.Text] =
                    completionSource;
            }

            Interlocked.Increment(
                ref _callCount
            );

            // 故意忽略 cancellationToken，
            // 模拟某些已经发出去、无法立即停止的请求。
            return completionSource.Task;
        }

        public void Complete(
            string sourceText,
            string translatedText
        )
        {
            TaskCompletionSource<TranslationResult>
                completionSource;

            lock (_lock)
            {
                completionSource =
                    _pending[sourceText];
            }

            completionSource.SetResult(
                new TranslationResult(
                    translatedText,
                    "zh"
                )
            );
        }
    }

    private sealed class ThrowingTranslationService
        : ITranslationService
    {
        private readonly Exception _exception;

        public ThrowingTranslationService(
            Exception exception
        )
        {
            _exception = exception;
        }

        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromException<TranslationResult>(
                _exception
            );
        }
    }
    private sealed class RecoverableTranslationService
        : ITranslationService
    {
        private int _callCount;

        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var callCount =
                Interlocked.Increment(
                    ref _callCount
                );

            if (callCount == 1)
            {
                return Task.FromException<TranslationResult>(
                    new TranslationException(
                        TranslationErrorKind.Network,
                        "Simulated network failure."
                    )
                );
            }

            return Task.FromResult(
                new TranslationResult(
                    "Success",
                    "zh"
                )
            );
        }
    }
}