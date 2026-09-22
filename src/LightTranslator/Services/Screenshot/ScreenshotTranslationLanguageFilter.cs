using System.Globalization;
using System.Text;

namespace LightTranslator.Services.Screenshot;

public static class ScreenshotTranslationLanguageFilter
{
    private const int MinimumLetterCount = 2;

    public static bool ShouldTranslate(
        string text,
        string targetLanguage
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var counts =
            CountScripts(
                text
            );

        if (counts.TotalLetters < MinimumLetterCount)
        {
            return false;
        }

        return
            targetLanguage
                .Trim()
                .ToLowerInvariant() switch
            {
                "zh" =>
                    !(
                        counts.Han >= MinimumLetterCount &&
                        counts.Kana == 0 &&
                        counts.OtherLetters == 0 &&
                        counts.Han >=
                        counts.LatinWords * 2
                    ),

                "en" =>
                    !(
                        counts.Latin >= MinimumLetterCount &&
                        counts.Han == 0 &&
                        counts.Kana == 0 &&
                        counts.OtherLetters == 0
                    ),

                "ja" =>
                    !(
                        counts.Kana > 0 &&
                        counts.OtherLetters == 0 &&
                        counts.Han +
                        counts.Kana >=
                        counts.LatinWords * 2
                    ),

                _ =>
                    true
            };
    }

    private static ScriptCounts CountScripts(
        string text
    )
    {
        var han = 0;
        var kana = 0;
        var latin = 0;
        var latinWords = 0;
        var otherLetters = 0;
        var insideLatinWord = false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsLatin(rune.Value))
            {
                latin++;

                if (!insideLatinWord)
                {
                    latinWords++;
                    insideLatinWord = true;
                }

                continue;
            }

            if (
                insideLatinWord &&
                IsLatinWordContinuation(
                    rune
                )
            )
            {
                continue;
            }

            insideLatinWord = false;

            if (IsHan(rune.Value))
            {
                han++;

                continue;
            }

            if (IsKana(rune.Value))
            {
                kana++;

                continue;
            }

            if (IsLetter(rune))
            {
                otherLetters++;
            }
        }

        return
            new ScriptCounts(
                han,
                kana,
                latin,
                latinWords,
                otherLetters
            );
    }

    private static bool IsHan(
        int value
    )
    {
        return
            value is >= 0x3400 and <= 0x4DBF or
            >= 0x4E00 and <= 0x9FFF or
            >= 0xF900 and <= 0xFAFF or
            >= 0x20000 and <= 0x3134F;
    }

    private static bool IsKana(
        int value
    )
    {
        return
            value is >= 0x3040 and <= 0x30FF or
            >= 0x31F0 and <= 0x31FF or
            >= 0xFF66 and <= 0xFF9D;
    }

    private static bool IsLatin(
        int value
    )
    {
        return
            value is >= 0x0041 and <= 0x005A or
            >= 0x0061 and <= 0x007A or
            >= 0x00C0 and <= 0x024F or
            >= 0x1E00 and <= 0x1EFF or
            >= 0xFF21 and <= 0xFF3A or
            >= 0xFF41 and <= 0xFF5A;
    }

    private static bool IsLetter(
        Rune rune
    )
    {
        return
            Rune.GetUnicodeCategory(rune) is
                UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter;
    }

    private static bool IsLatinWordContinuation(
        Rune rune
    )
    {
        return
            Rune.GetUnicodeCategory(rune) ==
            UnicodeCategory.DecimalDigitNumber ||
            rune.Value is
                '-' or
                '_' or
                '.';
    }

    private readonly record struct ScriptCounts(
        int Han,
        int Kana,
        int Latin,
        int LatinWords,
        int OtherLetters
    )
    {
        public int TotalLetters =>
            Han +
            Kana +
            Latin +
            OtherLetters;
    }
}
