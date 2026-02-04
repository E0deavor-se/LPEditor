using System.Linq;
using LPEditorApp.Models.Ai;
using LPEditorApp.Utils;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiGenerateDecorationService
{
    private readonly IAiChatClient _chatClient;
    private readonly AiDecorationValidator _validator;
    private readonly AiOptions _options;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateDecorationService(IAiChatClient chatClient, AiDecorationValidator validator, IOptions<AiOptions> options, LPEditorApp.Utils.ILogger logger)
    {
        _chatClient = chatClient;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiDecorationOutcome> GenerateDecorationAsync(AiDesignRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return AiDecorationOutcome.Failed("AIの設定が未完了です。管理者にお問い合わせください。", new List<string> { "api key missing" });
        }

        var maxRetry = ResolveMaxRetry();
        var errors = new List<string>();

        for (var attempt = 1; attempt <= maxRetry + 1; attempt++)
        {
            var messages = BuildMessages(request, errors, attempt > 1);
            try
            {
                var model = ResolveDecorModel();
                var content = await _chatClient.CreateChatCompletionAsync(model, messages, strictJsonOnly: true, cancellationToken);

                if (_options.MaxAiResponseChars > 0 && content.Length > _options.MaxAiResponseChars)
                {
                    errors = new List<string> { "response too large" };
                    LogFailure(request, content, errors, attempt, "response too large");
                    continue;
                }

                var (spec, validation) = _validator.TryParseAndValidate(content);
                if (validation.IsValid)
                {
                    var normalized = validation.NormalizedSpec ?? spec;
                    if (normalized is not null)
                    {
                        return AiDecorationOutcome.Success(normalized, validation.Warnings, content);
                    }
                    errors = new List<string> { "validated but spec is null" };
                    LogFailure(request, content, errors, attempt, "validation returned null");
                    continue;
                }

                errors = validation.Errors.ToList();
                LogFailure(request, content, errors, attempt, "validation failed");
            }
            catch (Exception ex)
            {
                errors = new List<string> { ex.Message };
                LogFailure(request, string.Empty, errors, attempt, "exception");
            }
        }

        var userMessage = AiErrorClassifier.GetUserMessage(string.Join(" | ", errors));
        return AiDecorationOutcome.Failed(userMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。", errors);
    }

    private List<OpenAiMessage> BuildMessages(AiDesignRequest request, List<string> errors, bool isRetry)
    {
                var system = @"
あなたはLP装飾の設計アシスタントです。
出力はJSONのみ。コードフェンスや説明は絶対に付けない。
必ず以下のJSON Schemaに完全準拠し、未知のフィールドは入れない。
装飾は業務LP向けに控えめで読みやすさ最優先。
スマホで破綻しないパターンと余白を選ぶ。

Schema:
{
    ""background"": {
        ""type"": ""solid|gradient|pattern"",
        ""colors"": [""#RRGGBB"", ""#RRGGBB""],
        ""pattern"": ""dots|waves|none"",
        ""opacity"": 0.0
    },
    ""sectionFrame"": {
        ""style"": ""card|flat|band"",
        ""radius"": 0,
        ""shadow"": ""none|soft|medium"",
        ""border"": ""none|light""
    },
    ""headingDecoration"": {
        ""type"": ""none|accent-line|pill|label"",
        ""color"": ""#RRGGBB"",
        ""thickness"": 0
    },
    ""ctaEmphasis"": {
        ""style"": ""none|badge|glow"",
        ""color"": ""#RRGGBB""
    },
    ""sectionDivider"": {
        ""type"": ""none|wave|zigzag"",
        ""height"": 0,
        ""color"": ""#RRGGBB""
    }
}
";

        var user = $@"
【入力】
業種: {request.Industry}
キャンペーン種別: {request.CampaignType}
トーン: {request.Tone}
ブランドカラー希望: {request.BrandColorHint}
参考URL: {request.ReferenceUrl}
禁止事項: {request.Prohibited}
";

        user = TrimInput(user);

        if (isRetry && errors.Count > 0)
        {
            var errorLines = string.Join("\n", errors.Select(e => $"- {e}"));
            user += $@"

【再生成の指示】
前回の出力は以下の理由で無効でした：
{errorLines}
同じSchemaで、上記を必ず修正してください。
出力はJSONのみ。説明文禁止。未知フィールド禁止。
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
            return Math.Clamp(_options.MaxRetryCount, 0, 3);
        }

        return Math.Clamp(_options.MaxRetries, 0, 3);
    }

    private string ResolveDecorModel()
    {
        if (!string.IsNullOrWhiteSpace(_options.ModelDecorationSpec))
        {
            return _options.ModelDecorationSpec;
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

    private void LogFailure(AiDesignRequest request, string raw, List<string> errors, int attempt, string reason)
    {
        var maskedInput = MaskSensitive(BuildInputSummary(request));
        var maskedRaw = MaskSensitive(TrimLong(raw));
        var errorText = errors is null || errors.Count == 0 ? "(none)" : string.Join(" | ", errors);
        _logger.Warn($"[AI-Decor] {reason} (attempt {attempt}): {errorText}");
        _logger.Warn($"[AI-Decor] input: {maskedInput}");
        if (!string.IsNullOrWhiteSpace(maskedRaw))
        {
            _logger.Warn($"[AI-Decor] response: {maskedRaw}");
        }
    }

    private static string BuildInputSummary(AiDesignRequest request)
    {
        return string.Join(" ", new[]
        {
            request.Industry,
            request.CampaignType,
            request.Tone,
            request.BrandColorHint,
            request.ReferenceUrl,
            request.Prohibited
        }.Where(text => !string.IsNullOrWhiteSpace(text)));
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

public record AiDecorationOutcome(bool IsSuccess, LpDecorationSpec? Spec, IReadOnlyList<string> Warnings, string? RawJson, IReadOnlyList<string> Errors, string? UserMessage)
{
    public static AiDecorationOutcome Success(LpDecorationSpec spec, IReadOnlyList<string> warnings, string raw)
        => new(true, spec, warnings, raw, Array.Empty<string>(), null);

    public static AiDecorationOutcome Failed(string userMessage, IReadOnlyList<string> errors)
        => new(false, null, Array.Empty<string>(), null, errors, userMessage);
}
