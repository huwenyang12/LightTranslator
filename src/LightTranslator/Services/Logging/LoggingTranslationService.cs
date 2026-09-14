using System.Diagnostics;
using LightTranslator.Models;
using LightTranslator.Services.Translation;

namespace LightTranslator.Services.Logging;

public sealed class LoggingTranslationService
    : ITranslationService
{
    private readonly ITranslationService _innerService;
    private readonly AppLogger _logger;


    public LoggingTranslationService(
        ITranslationService innerService,
        AppLogger logger
    )
    {
        _innerService =
            innerService;

        _logger =
            logger;
    }


    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch =
            Stopwatch.StartNew();

        try
        {
            var result =
                await _innerService.TranslateAsync(
                    request,
                    cancellationToken
                );

            stopwatch.Stop();

            _logger.TranslationCompleted(
                stopwatch.ElapsedMilliseconds
            );

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.TranslationFailed(
                exception
            );

            throw;
        }
    }
}