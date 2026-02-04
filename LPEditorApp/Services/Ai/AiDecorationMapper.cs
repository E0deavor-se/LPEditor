using LPEditorApp.Models.Ai;

namespace LPEditorApp.Services.Ai;

public class AiDecorationMapper
{
    public AiDecorationMapping Map(LpDecorationSpec spec)
    {
        var colors = spec.Background.Colors ?? new List<string> { "#F8FAFC", "#FFFFFF" };
        if (colors.Count == 1)
        {
            colors.Add(colors[0]);
        }

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ai-decor-bg-1"] = colors[0],
            ["--ai-decor-bg-2"] = colors.Count > 1 ? colors[1] : colors[0],
            ["--ai-decor-bg-opacity"] = spec.Background.Opacity.ToString("0.###"),
            ["--ai-decor-frame-radius"] = $"{Math.Clamp(spec.SectionFrame.Radius, 0, 28)}px",
            ["--ai-decor-heading-color"] = spec.HeadingDecoration.Color,
            ["--ai-decor-heading-thickness"] = $"{Math.Clamp(spec.HeadingDecoration.Thickness, 0, 8)}px",
            ["--ai-decor-cta-color"] = spec.CtaEmphasis.Color,
            ["--ai-decor-divider-height"] = $"{Math.Clamp(spec.SectionDivider.Height, 0, 48)}px",
            ["--ai-decor-divider-color"] = spec.SectionDivider.Color
        };

        var classes = new List<string>
        {
            $"ai-decor-bg-{spec.Background.Type}",
            $"ai-decor-pattern-{spec.Background.Pattern}",
            $"ai-decor-frame-{spec.SectionFrame.Style}",
            $"ai-decor-shadow-{spec.SectionFrame.Shadow}",
            $"ai-decor-border-{spec.SectionFrame.Border}",
            $"ai-decor-heading-{spec.HeadingDecoration.Type}",
            $"ai-decor-cta-{spec.CtaEmphasis.Style}",
            $"ai-decor-divider-{spec.SectionDivider.Type}"
        };

        return new AiDecorationMapping(variables, classes);
    }
}

public record AiDecorationMapping(IReadOnlyDictionary<string, string> Variables, IReadOnlyList<string> Classes);
