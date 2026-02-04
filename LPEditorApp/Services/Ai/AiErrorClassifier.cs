namespace LPEditorApp.Services.Ai;

public static class AiErrorClassifier
{
    public static string? GetUserMessage(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var lower = error.ToLowerInvariant();
        if (lower.Contains("insufficient_quota") || (lower.Contains("429") && lower.Contains("quota")))
        {
            return "OpenAIのクレジット/課金が不足しています。";
        }
        if (lower.Contains("rate_limit") || lower.Contains("too many requests"))
        {
            return "アクセスが混雑しています。時間をおいて再試行してください。";
        }
        if (lower.Contains("401") || lower.Contains("unauthorized"))
        {
            return "APIキーが無効です。管理者にお問い合わせください。";
        }
        if (lower.Contains("403") || lower.Contains("forbidden"))
        {
            return "APIキーの権限が不足しています。";
        }

        return null;
    }
}
