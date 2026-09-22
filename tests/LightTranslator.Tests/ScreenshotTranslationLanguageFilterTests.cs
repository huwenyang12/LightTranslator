using LightTranslator.Services.Screenshot;

namespace LightTranslator.Tests;

public sealed class ScreenshotTranslationLanguageFilterTests
{
    [Theory]
    [InlineData("Try DeepSeek 中文", "zh", true)]
    [InlineData("帮我用 DeepSeek 想个翻译软件的中文名", "zh", false)]
    [InlineData("使用 DeepSeek-V3", "zh", false)]
    [InlineData("翻訳アプリの名前", "zh", true)]
    [InlineData("使用 DeepSeek", "en", true)]
    [InlineData("Use DeepSeek", "en", false)]
    [InlineData("ＨＥＬＬＯ", "en", false)]
    [InlineData("翻訳アプリ", "ja", false)]
    [InlineData("DeepSeek-V3を使用", "ja", false)]
    [InlineData("Tell me your topic", "ja", true)]
    [InlineData("A", "zh", false)]
    [InlineData("123", "zh", false)]
    public void ShouldTranslate_UsesDominantScriptWithoutLettingBrandNamesWin(
        string text,
        string targetLanguage,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            ScreenshotTranslationLanguageFilter.ShouldTranslate(
                text,
                targetLanguage
            )
        );
    }
}
