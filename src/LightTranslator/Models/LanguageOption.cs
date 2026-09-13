namespace LightTranslator.Models;

public sealed record LanguageOption(
    string Code,
    string DisplayName
)
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