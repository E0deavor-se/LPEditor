using System.Text.RegularExpressions;

namespace LPEditorApp.Services;

public class JsReplacementService
{
    private static readonly Regex CountdownRegex = new(
        "(?<prefix>endDate\\s*=\\s*new\\s+Date)\\s*\\(\\s*['\"][^'\"]+['\"]\\s*\\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string ReplaceCountdownEnd(string jsContent, string countdownEnd)
    {
        if (string.IsNullOrWhiteSpace(jsContent))
        {
            return jsContent;
        }

        var normalized = NormalizeCountdownEnd(countdownEnd);

        return CountdownRegex.Replace(jsContent, match =>
        {
            var prefix = match.Groups["prefix"].Value;
            return $"{prefix}('{normalized}')";
        });
    }

    private static string NormalizeCountdownEnd(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss");
        }

        return value;
    }
}
