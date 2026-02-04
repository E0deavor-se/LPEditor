using System.IO.Compression;
using System.Text.RegularExpressions;
using LPEditorApp.Models.Ai;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiGenerateReferenceZipService
{
    private readonly IAiChatClient _chatClient;
    private readonly AiOptions _options;
    private readonly LPEditorApp.Utils.ILogger _logger;

    private static readonly Regex ScriptTag = new(@"<\s*script\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExternalUrl = new("https?://", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AiGenerateReferenceZipService(IAiChatClient chatClient, IOptions<AiOptions> options, LPEditorApp.Utils.ILogger logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiReferenceZipOutcome> GenerateAsync(AiReferenceDesignRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return AiReferenceZipOutcome.Failed("AIの設定が未完了です。管理者にお問い合わせください。", new List<string> { "api key missing" });
        }

        var maxRetry = ResolveMaxRetry();
        var errors = new List<string>();

        for (var attempt = 1; attempt <= maxRetry + 1; attempt++)
        {
            var messages = BuildMessages(request, errors, attempt > 1);
            try
            {
                var model = ResolveReferenceZipModel();
                var content = await _chatClient.CreateChatCompletionAsync(model, messages, strictJsonOnly: false, cancellationToken);

                if (_options.MaxAiResponseChars > 0 && content.Length > _options.MaxAiResponseChars)
                {
                    errors = new List<string> { "response too large" };
                    LogFailure(request, content, errors, attempt, "response too large");
                    continue;
                }

                if (!TrySplitHtmlCss(content, out var html, out var css))
                {
                    errors = new List<string> { "html/css split failed" };
                    LogFailure(request, content, errors, attempt, "split failed");
                    continue;
                }

                var validateErrors = ValidateHtmlCss(html, css);
                if (validateErrors.Count > 0)
                {
                    errors = validateErrors;
                    LogFailure(request, content, errors, attempt, "validation failed");
                    continue;
                }

                var zip = BuildZip(html, css);
                return AiReferenceZipOutcome.Success(zip, html, css);
            }
            catch (Exception ex)
            {
                errors = new List<string> { ex.Message };
                LogFailure(request, string.Empty, errors, attempt, "exception");
            }
        }

        var userMessage = AiErrorClassifier.GetUserMessage(string.Join(" | ", errors));
        return AiReferenceZipOutcome.Failed(userMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。", errors);
    }

    private List<OpenAiMessage> BuildMessages(AiReferenceDesignRequest request, List<string> errors, bool isRetry)
    {
        var system = @"
あなたはLPのHTML/CSSを生成するアシスタントです。
出力は index.html と styles.css の2ブロックのみ。
以下の区切り文字を必ず使う:
===index.html===
...HTML...
===styles.css===
...CSS...
外部CDN禁止。script禁止。レスポンシブ対応必須。
参考URLのHTML/CSSや文章/画像をコピーしない。
雰囲気を抽象化して、要素の分解と再構成に留める。
";

        var user = $@"
【入力】
参考URL: {request.ReferenceUrl}
キャンペーン種別: {request.CampaignType}
トーン: {request.Tone}
ブランドカラー希望: {request.BrandColorHint}

【指示】
- セクション構成は一般的なLP（Hero/説明/特典/注意/CTA）を基本にする
- 似せすぎ禁止。色・レイアウト・装飾は""近い雰囲気""に留める
";

        user = TrimInput(user);

        if (isRetry && errors.Count > 0)
        {
            var errorLines = string.Join("\n", errors.Select(e => $"- {e}"));
            user += $@"

【再生成の指示】
前回の出力は以下の理由で無効でした：
{errorLines}
同じ形式で修正してください。外部URL・scriptは禁止。
";
        }

        return new List<OpenAiMessage>
        {
            new() { Role = "system", Content = system.Trim() },
            new() { Role = "user", Content = user.Trim() }
        };
    }

    private int ResolveMaxRetry()
    {
        if (_options.MaxRetryCount > 0)
        {
            return Math.Clamp(_options.MaxRetryCount, 0, 2);
        }

        return Math.Clamp(_options.MaxRetries, 0, 2);
    }

    private string ResolveReferenceZipModel()
    {
        if (!string.IsNullOrWhiteSpace(_options.ModelReferenceZip))
        {
            return _options.ModelReferenceZip;
        }

        return _options.Model;
    }

    private string TrimInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var max = Math.Clamp(_options.MaxInputLength, 200, 10000);
        if (value.Length <= max)
        {
            return value;
        }

        return value[..max] + "...";
    }

    private static bool TrySplitHtmlCss(string content, out string html, out string css)
    {
        html = string.Empty;
        css = string.Empty;
        var indexMarker = "===index.html===";
        var cssMarker = "===styles.css===";
        var indexPos = content.IndexOf(indexMarker, StringComparison.OrdinalIgnoreCase);
        var cssPos = content.IndexOf(cssMarker, StringComparison.OrdinalIgnoreCase);
        if (indexPos < 0 || cssPos < 0 || cssPos <= indexPos)
        {
            return false;
        }

        html = content[(indexPos + indexMarker.Length)..cssPos].Trim();
        css = content[(cssPos + cssMarker.Length)..].Trim();
        return !string.IsNullOrWhiteSpace(html) && !string.IsNullOrWhiteSpace(css);
    }

    internal static List<string> ValidateHtmlCss(string html, string css)
    {
        var errors = new List<string>();
        if (ScriptTag.IsMatch(html) || ScriptTag.IsMatch(css))
        {
            errors.Add("script tag is not allowed");
        }
        if (ExternalUrl.IsMatch(html) || ExternalUrl.IsMatch(css))
        {
            errors.Add("external url is not allowed");
        }
        return errors;
    }

    private static byte[] BuildZip(string html, string css)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var htmlEntry = archive.CreateEntry("index.html");
            using (var writer = new StreamWriter(htmlEntry.Open()))
            {
                writer.Write(html);
            }

            var cssEntry = archive.CreateEntry("styles.css");
            using (var writer = new StreamWriter(cssEntry.Open()))
            {
                writer.Write(css);
            }
        }

        return stream.ToArray();
    }

    private void LogFailure(AiReferenceDesignRequest request, string raw, List<string> errors, int attempt, string reason)
    {
        var maskedRaw = MaskSensitive(TrimLong(raw));
        var errorText = errors is null || errors.Count == 0 ? "(none)" : string.Join(" | ", errors);
        _logger.Warn($"[AI-RefZip] {reason} (attempt {attempt}): {errorText}");
        if (!string.IsNullOrWhiteSpace(maskedRaw))
        {
            _logger.Warn($"[AI-RefZip] response: {maskedRaw}");
        }
    }

    private static string TrimLong(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= 1200)
        {
            return value;
        }

        return value[..800] + "..." + value[^200..];
    }

    private static string MaskSensitive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var masked = System.Text.RegularExpressions.Regex.Replace(raw, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "[email]");
        masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\d{2,4}-\d{2,4}-\d{3,4}", "[phone]");
        masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\d{8,}", "[number]");
        return masked.Length > 1200 ? masked[..1200] + "..." : masked;
    }
}

public record AiReferenceZipOutcome(bool IsSuccess, byte[]? ZipBytes, string? Html, string? Css, IReadOnlyList<string> Errors, string? UserMessage)
{
    public static AiReferenceZipOutcome Success(byte[] zip, string html, string css) => new(true, zip, html, css, Array.Empty<string>(), null);
    public static AiReferenceZipOutcome Failed(string message, IReadOnlyList<string> errors) => new(false, null, null, null, errors, message);
}
