using LPEditorApp.Models.Ai;

namespace LPEditorApp.Services.Ai;

public class AiReferenceStyleMapper
{
    public AiReferenceStyleMapping Map(LpReferenceStyleSpec spec)
    {
        var t = spec.StyleTokens;
        var colors = t.Colors;
        var typography = t.Typography;
        var spacing = t.Spacing;
        var radius = t.Radius;

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ref-primary"] = colors.Primary,
            ["--ref-accent"] = colors.Accent,
            ["--ref-bg"] = colors.Bg,
            ["--ref-text"] = colors.Text,
            ["--ref-muted"] = colors.Muted,
            ["--ref-border"] = colors.Border,
            ["--ref-h1"] = $"{typography.H1}px",
            ["--ref-h2"] = $"{typography.H2}px",
            ["--ref-body"] = $"{typography.Body}px",
            ["--ref-small"] = $"{typography.Small}px",
            ["--ref-section-y"] = $"{spacing.SectionY}px",
            ["--ref-card-padding"] = $"{spacing.CardPadding}px",
            ["--ref-grid-gap"] = $"{spacing.GridGap}px",
            ["--ref-radius-card"] = $"{radius.Card}px",
            ["--ref-radius-button"] = $"{radius.Button}px",
            ["--ref-radius-badge"] = $"{radius.Badge}px"
        };

        var classes = new List<string>
        {
            $"ref-hero-{spec.LayoutRecipe.Hero}",
            $"ref-section-{spec.LayoutRecipe.Section}",
            $"ref-heading-{spec.LayoutRecipe.Heading}",
            $"ref-ranking-{spec.LayoutRecipe.Ranking}",
            $"ref-notes-{spec.LayoutRecipe.Notes}",
            $"ref-bg-{spec.DecorSpec.Background}",
            $"ref-divider-{spec.DecorSpec.Divider}",
            $"ref-badge-{spec.DecorSpec.Badge}",
            $"ref-weight-{spec.StyleTokens.Typography.WeightScale}"
        };

        return new AiReferenceStyleMapping(variables, classes);
    }
}

public record AiReferenceStyleMapping(IReadOnlyDictionary<string, string> Variables, IReadOnlyList<string> Classes);
