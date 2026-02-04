using System.Linq;
using LPEditorApp.Models.Ai;
using LPEditorApp.Utils;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiGenerateLpService
{
    private readonly IAiChatClient _chatClient;
    private readonly AiLpBlueprintValidator _validator;
    private readonly AiOptions _options;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateLpService(IAiChatClient chatClient, AiLpBlueprintValidator validator, IOptions<AiOptions> options, LPEditorApp.Utils.ILogger logger)
    {
        _chatClient = chatClient;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiGenerateOutcome> GenerateBlueprintAsync(AiGenerateLpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return AiGenerateOutcome.Failed("AIの設定が未完了です。管理者にお問い合わせください。", new List<string> { "api key missing" });
        }

        var maxRetry = ResolveMaxRetry();
        var errors = new List<string>();

        for (var attempt = 1; attempt <= maxRetry + 1; attempt++)
        {
            var messages = BuildMessages(request, errors, attempt > 1);
            try
            {
                var model = ResolveBlueprintModel();
                var content = await _chatClient.CreateChatCompletionAsync(model, messages, _options.StrictJsonOnly, cancellationToken);

                if (_options.MaxAiResponseChars > 0 && content.Length > _options.MaxAiResponseChars)
                {
                    errors = new List<string> { "response too large" };
                    LogFailure(request, content, errors, attempt, "response too large");
                    continue;
                }

                var (blueprint, validation) = _validator.TryParseAndValidate(content);
                if (validation.IsValid)
                {
                    var normalized = validation.NormalizedBlueprint ?? blueprint;
                    if (normalized is not null)
                    {
                        return AiGenerateOutcome.Success(normalized, validation.Warnings, content);
                    }
                    errors = new List<string> { "validated but blueprint is null" };
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

        return AiGenerateOutcome.Failed("AI生成に失敗しました。入力内容を見直して再度お試しください。", errors);
    }

    private List<OpenAiMessage> BuildMessages(AiGenerateLpRequest request, List<string> errors, bool isRetry)
    {
        var system = @"
あなたはLP（ランディングページ）の構成設計アシスタントです。
出力はJSONのみ。コードフェンスや説明は絶対に付けない。
必ず以下のJSON Schemaに完全準拠し、未知のフィールドは入れない。
日本語で生成する。
コピーは短く強く、箇条書き中心。注意事項は丁寧語。
断定表現（必ず・絶対・日本一 等）は避け、誤認防止の注記を適宜入れる。
固有名詞は入力に含まれる場合のみ使用し、なければ一般化する。
このフェーズでは最小構成のみ生成する：hero, offer, howto, notes, footer の5セクション。

Schema:
{
  ""meta"": {
    ""language"": ""ja"",
    ""title"": ""string"",
    ""tone"": ""casual|formal"",
    ""goal"": ""acquisition|activation|retention|revenue"",
    ""industry"": ""string"",
    ""brand"": { ""name"": ""string"", ""colorHint"": ""string|null"" }
  },
  ""sections"": [
    {
      ""type"": ""hero|benefits|howto|offer|ranking|faq|notes|footer"",
      ""id"": ""string"",
      ""props"": {
        ""heading"": ""string|null"",
        ""subheading"": ""string|null"",
        ""body"": ""string|null"",
        ""bullets"": [""string""],
        ""ctaText"": ""string|null"",
        ""disclaimer"": ""string|null"",
        ""items"": [
          { ""title"": ""string"", ""text"": ""string"", ""badge"": ""string|null"" }
        ]
      }
    }
  ]
}

ルール:
- 必須セクション: hero, offer, howto, notes, footer
- offer.props.items に「特典内容」「利用条件」「期間」を必ず入れる
- notes.props.bullets は注意事項を5〜10個
- footer は問い合わせ・会社情報を一般形で短く
";

    var user = $@"
【入力】
業種: {request.Industry}
会社名・ブランド名: {request.BrandName}
キャンペーン概要: {request.CampaignOverview}
オファー: {request.Offer}
条件: {request.Conditions}
期間: {request.Period}
対象者: {request.Target}
トーン: {request.Tone}
目的: {request.Goal}
必須文言: {request.RequiredStatements}
禁止表現: {request.ProhibitedExpressions}
注意点: {request.Notes}
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
出力はJSONのみ。説明文禁止。空文字は禁止、空ならnull。
未知のフィールドは禁止。
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

    private string ResolveBlueprintModel()
    {
        if (!string.IsNullOrWhiteSpace(_options.ModelBlueprint))
        {
            return _options.ModelBlueprint;
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

    private void LogFailure(AiGenerateLpRequest request, string raw, List<string> errors, int attempt, string reason)
    {
        var maskedInput = MaskSensitive(BuildInputSummary(request));
        var maskedRaw = MaskSensitive(TrimLong(raw));
        var errorText = errors is null || errors.Count == 0 ? "(none)" : string.Join(" | ", errors);
        _logger.Warn($"[AI] {reason} (attempt {attempt}): {errorText}");
        _logger.Warn($"[AI] input: {maskedInput}");
        if (!string.IsNullOrWhiteSpace(maskedRaw))
        {
            _logger.Warn($"[AI] response: {maskedRaw}");
        }
    }

    private static string BuildInputSummary(AiGenerateLpRequest request)
    {
        return string.Join(" ", new[]
        {
            request.Industry,
            request.BrandName,
            request.CampaignOverview,
            request.Offer,
            request.Conditions,
            request.Period,
            request.Target,
            request.Notes,
            request.RequiredStatements,
            request.ProhibitedExpressions
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

public record AiGenerateOutcome(bool IsSuccess, LpBlueprint? Blueprint, IReadOnlyList<string> Warnings, string? RawJson, IReadOnlyList<string> Errors, string? UserMessage)
{
    public static AiGenerateOutcome Success(LpBlueprint blueprint, IReadOnlyList<string> warnings, string raw)
        => new(true, blueprint, warnings, raw, Array.Empty<string>(), null);

    public static AiGenerateOutcome Failed(string userMessage, IReadOnlyList<string> errors)
        => new(false, null, Array.Empty<string>(), null, errors, userMessage);
}
