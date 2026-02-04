using LPEditorApp.Models.Ai;

namespace LPEditorApp.Services.Ai;

public class AiDesignMapper
{
    public AiDesignMapping Map(LpDesignSpec spec)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ai-primary"] = spec.Theme.Primary,
            ["--ai-secondary"] = spec.Theme.Secondary,
            ["--ai-accent"] = spec.Theme.Accent,
            ["--ai-bg"] = spec.Theme.Bg,
            ["--ai-text"] = spec.Theme.Text,
            ["--ai-radius"] = $"{Math.Clamp(spec.Theme.Radius, 0, 32)}px",
            ["--ai-font"] = ResolveFont(spec.Theme.Font)
        };

        var classes = new List<string>
        {
            $"ai-container-{spec.Layout.Container}",
            $"ai-hero-{spec.Layout.Hero}",
            $"ai-section-{spec.Layout.SectionStyle}",
            $"ai-heading-{spec.Layout.HeadingStyle}",
            $"ai-offer-{spec.Layout.OfferStyle}",
            $"ai-howto-{spec.Layout.HowtoStyle}",
            $"ai-notes-{spec.Layout.NotesStyle}",
            $"ai-ranking-{spec.Layout.RankingStyle}",
            $"ai-shadow-{spec.Theme.Shadow}",
            $"ai-font-{spec.Theme.Font}",
            $"ai-cta-{spec.Theme.CtaStyle}"
        };

        return new AiDesignMapping(variables, classes);
    }

    private static string ResolveFont(string? font)
    {
        return font?.ToLowerInvariant() switch
        {
            "rounded" => "'M PLUS Rounded 1c', 'Hiragino Maru Gothic ProN', 'Segoe UI', sans-serif",
            "gothic" => "'Noto Sans JP', 'Yu Gothic', 'Segoe UI', sans-serif",
            _ => "'Inter', 'Segoe UI', 'Helvetica Neue', Arial, sans-serif"
        };
    }
}

public record AiDesignMapping(IReadOnlyDictionary<string, string> Variables, IReadOnlyList<string> Classes);
