using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Wisp2Win.Services;

public sealed class TranscriptPostProcessor
{
    public string Polish(string text, string language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var polished = text.Trim();
        polished = ReplaceSpokenPunctuation(polished, language);
        polished = NormalizePunctuationSpacing(polished);
        polished = CapitalizeSentences(polished, language);
        polished = EnsureFinalPunctuation(polished);
        AppLog.Info("polish", $"Polished chars {text.Length}->{polished.Length}");
        return polished;
    }

    private static string ReplaceSpokenPunctuation(string text, string language)
    {
        var result = text;
        var isRussian = language.Equals("ru", StringComparison.OrdinalIgnoreCase)
            || language.Equals("russian", StringComparison.OrdinalIgnoreCase)
            || language.Equals("auto", StringComparison.OrdinalIgnoreCase);

        if (isRussian)
        {
            result = ReplacePhrase(result, "точка с запятой", ";");
            result = ReplacePhrase(result, "вопросительный знак", "?");
            result = ReplacePhrase(result, "восклицательный знак", "!");
            result = ReplacePhrase(result, "новая строка", "\n");
            result = ReplacePhrase(result, "с новой строки", "\n");
            result = ReplacePhrase(result, "новый абзац", "\n\n");
            result = ReplacePhrase(result, "абзац", "\n\n");
            result = ReplacePhrase(result, "двоеточие", ":");
            result = ReplacePhrase(result, "запятая", ",");
            result = ReplacePhrase(result, "точка", ".");
            result = ReplacePhrase(result, "тире", "-");
        }
        else
        {
            result = ReplacePhrase(result, "semicolon", ";");
            result = ReplacePhrase(result, "question mark", "?");
            result = ReplacePhrase(result, "exclamation mark", "!");
            result = ReplacePhrase(result, "new paragraph", "\n\n");
            result = ReplacePhrase(result, "new line", "\n");
            result = ReplacePhrase(result, "colon", ":");
            result = ReplacePhrase(result, "comma", ",");
            result = ReplacePhrase(result, "period", ".");
            result = ReplacePhrase(result, "full stop", ".");
            result = ReplacePhrase(result, "dash", "-");
        }

        return result;
    }

    private static string ReplacePhrase(string text, string phrase, string replacement) =>
        Regex.Replace(text, $@"(?<!\p{{L}}){Regex.Escape(phrase)}(?!\p{{L}})", replacement, RegexOptions.IgnoreCase);

    private static string NormalizePunctuationSpacing(string text)
    {
        var result = text.Replace("\r\n", "\n").Replace('\r', '\n');
        result = Regex.Replace(result, @"[ \t]+", " ");
        result = Regex.Replace(result, @"\s+([,.;:!?])", "$1");
        result = Regex.Replace(result, @"([,.;:!?])(?=\S)", "$1 ");
        result = Regex.Replace(result, @"[ \t]*\n[ \t]*", "\n");
        result = Regex.Replace(result, @"\n{3,}", "\n\n");
        return result.Trim();
    }

    private static string CapitalizeSentences(string text, string language)
    {
        var culture = language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("ru-RU");

        var builder = new StringBuilder(text.Length);
        var sentenceStart = true;

        foreach (var ch in text)
        {
            if (sentenceStart && char.IsLetter(ch))
            {
                builder.Append(char.ToUpper(ch, culture));
                sentenceStart = false;
                continue;
            }

            builder.Append(ch);
            if (char.IsLetterOrDigit(ch))
            {
                sentenceStart = false;
            }
            else if (ch is '.' or '!' or '?' or '\n')
            {
                sentenceStart = true;
            }
        }

        return builder.ToString();
    }

    private static string EnsureFinalPunctuation(string text)
    {
        var trimmed = text.TrimEnd();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        var last = trimmed[^1];
        return last is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']' or '"' or '\'' ? trimmed : trimmed + ".";
    }
}
