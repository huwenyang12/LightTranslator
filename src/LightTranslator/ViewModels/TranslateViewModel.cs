using System.ComponentModel;
using System.Runtime.CompilerServices;
using LightTranslator.Models;
using LightTranslator.Services.Translation;

namespace LightTranslator.ViewModels;

public sealed class TranslateViewModel
    : INotifyPropertyChanged
{
    private readonly ITranslationService _translationService;
    private readonly TimeSpan _debounceDelay;

    private CancellationTokenSource? _translationCancellationTokenSource;

    private string _sourceText = string.Empty;
    private string _translatedText = string.Empty;
    private string _sourceLanguage = "auto";
    private string _targetLanguage = "zh";
    private string? _errorMessage;
    private string? _detectedSourceLanguage;

    public TranslateViewModel(
            ITranslationService translationService,
            TimeSpan? debounceDelay = null
        )
        {
            _translationService = translationService;

            _debounceDelay =
                debounceDelay ?? TimeSpan.FromMilliseconds(400);
        }

        public void SwapLanguages()
    {
        var oldSourceLanguage =
            SourceLanguage;

        var oldTargetLanguage =
            TargetLanguage;

        string? newTargetLanguage;

        if (oldSourceLanguage == "auto")
        {
            newTargetLanguage =
                _detectedSourceLanguage;

            if (
                string.IsNullOrWhiteSpace(
                    newTargetLanguage
                )
            )
            {
                return;
            }
        }
        else
        {
            newTargetLanguage =
                oldSourceLanguage;
        }

        _sourceLanguage =
            oldTargetLanguage;

        _targetLanguage =
            newTargetLanguage;

        OnPropertyChanged(
            nameof(SourceLanguage)
        );

        OnPropertyChanged(
            nameof(TargetLanguage)
        );

        if (
            !string.IsNullOrWhiteSpace(
                SourceText
            )
        )
        {
            ScheduleTranslation();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string SourceText
    {
        get => _sourceText;

        set
        {
            if (_sourceText == value)
            {
                return;
            }

            _sourceText = value;
            OnPropertyChanged();

            ScheduleTranslation();

        }
    }

    public string TranslatedText
    {
        get => _translatedText;

        private set
        {
            if (_translatedText == value)
            {
                return;
            }

            _translatedText = value;
            OnPropertyChanged();
        }
    }

    public string SourceLanguage
    {
        get => _sourceLanguage;

        set
        {
            if (_sourceLanguage == value)
            {
                return;
            }

            _sourceLanguage = value;
            OnPropertyChanged();

            if (!string.IsNullOrWhiteSpace(SourceText))
            {
                ScheduleTranslation();
            }
        }
    }

    public string TargetLanguage
    {
        get => _targetLanguage;

        set
        {
            if (_targetLanguage == value)
            {
                return;
            }

            _targetLanguage = value;
            OnPropertyChanged();

            if (!string.IsNullOrWhiteSpace(SourceText))
            {
                ScheduleTranslation();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ScheduleTranslation()
    {
        _translationCancellationTokenSource?.Cancel();

        _translationCancellationTokenSource =
            new CancellationTokenSource();

        _ = TranslateAfterDebounceAsync(
            _translationCancellationTokenSource.Token
        );
    }

    private async Task TranslateAfterDebounceAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(
                _debounceDelay,
                cancellationToken
            );

            var request = new TranslationRequest(
                SourceText,
                SourceLanguage,
                TargetLanguage
            );

            var result =
                await _translationService.TranslateAsync(
                    request,
                    cancellationToken
                );

            cancellationToken.ThrowIfCancellationRequested();

            _detectedSourceLanguage =
                result.DetectedSourceLanguage;

            ErrorMessage = null;
            TranslatedText = result.Text;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // 用户继续输入时，旧翻译任务正常取消。
        }
        catch (TranslationException ex)
        {
            ErrorMessage = ex.Kind switch
            {
                TranslationErrorKind.Configuration =>
                    "请先配置 DeepSeek API Key",

                TranslationErrorKind.InvalidApiKey =>
                    "DeepSeek API Key 无效",

                TranslationErrorKind.InsufficientBalance =>
                    "DeepSeek API 余额不足",

                TranslationErrorKind.RateLimited =>
                    "请求过于频繁，请稍后重试",

                TranslationErrorKind.Timeout =>
                    "翻译请求超时",

                TranslationErrorKind.Network =>
                    "网络连接失败",

                TranslationErrorKind.InvalidResponse =>
                    "翻译结果解析失败",

                _ =>
                    "翻译失败"
            };
        }
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null
    )
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName)
        );
    }
}