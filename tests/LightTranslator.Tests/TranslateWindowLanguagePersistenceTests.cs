using LightTranslator.Models;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

public class TranslateWindowLanguagePersistenceTests
{
    [Fact]
    public void Close_SavesCurrentLanguageSelection()
    {
        Exception? exception =
            null;

        string? savedSourceLanguage =
            null;

        string? savedTargetLanguage =
            null;

        var thread =
            new Thread(
                () =>
                {
                    try
                    {
                        var translationService =
                            new FakeTranslationService();

                        var persistence =
                            new FakeTextLanguageSettingsPersistence();

                        var viewModel =
                            new TranslateViewModel(
                                translationService,
                                initialSourceLanguage: "en",
                                initialTargetLanguage: "ja"
                            );

                        var window =
                            new TranslateWindow(
                                viewModel,
                                persistence
                            );

                        viewModel.SourceLanguage =
                            "ja";

                        viewModel.TargetLanguage =
                            "en";

                        window.Close();

                        savedSourceLanguage =
                            persistence.SavedSourceLanguage;

                        savedTargetLanguage =
                            persistence.SavedTargetLanguage;
                    }
                    catch (Exception ex)
                    {
                        exception =
                            ex;
                    }
                }
            );

        thread.SetApartmentState(
            ApartmentState.STA
        );

        thread.Start();
        thread.Join();

        Assert.Null(
            exception
        );

        Assert.Equal(
            "ja",
            savedSourceLanguage
        );

        Assert.Equal(
            "en",
            savedTargetLanguage
        );
    }

    private sealed class FakeTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult(
                    string.Empty,
                    null
                )
            );
        }
    }

    private sealed class FakeTextLanguageSettingsPersistence
        : ITextLanguageSettingsPersistence
    {
        public string? SavedSourceLanguage { get; private set; }

        public string? SavedTargetLanguage { get; private set; }

        public Task SaveAsync(
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken = default
        )
        {
            SavedSourceLanguage =
                sourceLanguage;

            SavedTargetLanguage =
                targetLanguage;

            return Task.CompletedTask;
        }
    }
}