using System.Text.Json;
using System.Text.RegularExpressions;
using LPEditorApp.Models.Ai;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiReferenceDesignValidator
{
    private readonly AiOptions _options;

    private static readonly HashSet<string> HeroTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "kv-image-top", "kv-split", "kv-poster"
    };

    private static readonly HashSet<string> SectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "card", "band", "flat"
    };

    private static readonly HashSet<string> HeadingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "band", "pill", "underline"
    };

    private static readonly HashSet<string> RankingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "table", "cards", "podium"
    };

    private static readonly HashSet<string> NotesTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "accordion", "boxed"
    };

    private static readonly HashSet<string> BackgroundTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "solid", "gradient"
    };

    private static readonly HashSet<string> DividerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "line", "wave"
    };

    private static readonly HashSet<string> BadgeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "label", "stamp"
    };

    private static readonly HashSet<string> WeightScales = new(StringComparer.OrdinalIgnoreCase)
    {
        "regular", "medium", "bold"
    };

    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private static readonly HashSet<int> SpacingScale = new() { 8, 16, 24, 32, 48 };

    public AiReferenceDesignValidator(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public AiReferenceDesignValidator() : this(Options.Create(new AiOptions()))
    {
    }

    public AiReferenceValidationResult Validate(LpReferenceStyleSpec? spec, JsonElement? raw = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (raw.HasValue)
        {
            ValidateNoUnknownFields(raw.Value, errors, warnings);
        }

        if (spec is null)
        {
            errors.Add("reference spec is null");
            return new AiReferenceValidationResult(errors, warnings, null);
        }

        if (spec.StyleTokens is null)
        {
            errors.Add("styleTokens is required");
        }
        else
        {
            ValidateColors(spec.StyleTokens.Colors, errors);
            ValidateTypography(spec.StyleTokens.Typography, errors);
            ValidateSpacing(spec.StyleTokens.Spacing, errors);
            ValidateRadius(spec.StyleTokens.Radius, errors);
            ValidateShadow(spec.StyleTokens.Shadow, errors);
        }

        if (spec.LayoutRecipe is null)
        {
            errors.Add("layoutRecipe is required");
        }
        else
        {
            ValidateEnum(spec.LayoutRecipe.Hero, HeroTypes, "layoutRecipe.hero", errors);
            ValidateEnum(spec.LayoutRecipe.Section, SectionTypes, "layoutRecipe.section", errors);
            ValidateEnum(spec.LayoutRecipe.Heading, HeadingTypes, "layoutRecipe.heading", errors);
            ValidateEnum(spec.LayoutRecipe.Ranking, RankingTypes, "layoutRecipe.ranking", errors);
            ValidateEnum(spec.LayoutRecipe.Notes, NotesTypes, "layoutRecipe.notes", errors);
        }

        if (spec.DecorSpec is null)
        {
            errors.Add("decorSpec is required");
        }
        else
        {
            ValidateEnum(spec.DecorSpec.Background, BackgroundTypes, "decorSpec.background", errors);
            ValidateEnum(spec.DecorSpec.Divider, DividerTypes, "decorSpec.divider", errors);
            ValidateEnum(spec.DecorSpec.Badge, BadgeTypes, "decorSpec.badge", errors);
        }

        if (errors.Count == 0)
        {
            var normalized = Normalize(spec, warnings);
            return new AiReferenceValidationResult(errors, warnings, normalized);
        }

        return new AiReferenceValidationResult(errors, warnings, null);
    }

    public (LpReferenceStyleSpec? Spec, AiReferenceValidationResult Result) TryParseAndValidate(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var spec = doc.RootElement.Deserialize<LpReferenceStyleSpec>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = Validate(spec, doc.RootElement);
            return (spec, result);
        }
        catch (Exception ex)
        {
            var result = new AiReferenceValidationResult(new List<string> { $"json parse failed: {ex.Message}" }, new List<string>(), null);
            return (null, result);
        }
    }

    private void ValidateNoUnknownFields(JsonElement root, List<string> errors, List<string> warnings)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("root must be object");
            return;
        }

        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "styleTokens", "layoutRecipe", "decorSpec"
        };

        foreach (var prop in root.EnumerateObject())
        {
            if (!rootFields.Contains(prop.Name))
            {
                AddUnknownField($"unknown root field: {prop.Name}", errors, warnings);
            }
        }

        ValidateUnknownObject(root, "styleTokens", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "colors", "typography", "spacing", "radius", "shadow"
        }, errors, warnings);

        ValidateUnknownObject(root, "layoutRecipe", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hero", "section", "heading", "ranking", "notes"
        }, errors, warnings);

        ValidateUnknownObject(root, "decorSpec", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "background", "divider", "badge"
        }, errors, warnings);
    }

    private void ValidateUnknownObject(JsonElement root, string name, HashSet<string> allowed, List<string> errors, List<string> warnings)
    {
        if (!root.TryGetProperty(name, out var obj) || obj.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in obj.EnumerateObject())
        {
            if (!allowed.Contains(prop.Name))
            {
                AddUnknownField($"unknown {name} field: {prop.Name}", errors, warnings);
            }
        }

        if (string.Equals(name, "styleTokens", StringComparison.OrdinalIgnoreCase))
        {
            ValidateUnknownNestedObject(obj, "colors", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "primary", "accent", "bg", "text", "muted", "border"
            }, errors, warnings);
            ValidateUnknownNestedObject(obj, "typography", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "h1", "h2", "body", "small", "weightScale"
            }, errors, warnings);
            ValidateUnknownNestedObject(obj, "spacing", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "sectionY", "cardPadding", "gridGap"
            }, errors, warnings);
            ValidateUnknownNestedObject(obj, "radius", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "card", "button", "badge"
            }, errors, warnings);
            ValidateUnknownNestedObject(obj, "shadow", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "card", "sticky"
            }, errors, warnings);
        }
    }

    private void ValidateUnknownNestedObject(JsonElement parent, string name, HashSet<string> allowed, List<string> errors, List<string> warnings)
    {
        if (!parent.TryGetProperty(name, out var obj) || obj.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in obj.EnumerateObject())
        {
            if (!allowed.Contains(prop.Name))
            {
                AddUnknownField($"unknown {name} field: {prop.Name}", errors, warnings);
            }
        }
    }

    private void AddUnknownField(string message, List<string> errors, List<string> warnings)
    {
        if (_options.StrictJsonOnly)
        {
            errors.Add(message);
        }
        else
        {
            warnings.Add(message);
        }
    }

    private static void ValidateEnum(string? value, HashSet<string> allowed, string name, List<string> errors)
    {
        if (!allowed.Contains(value ?? string.Empty))
        {
            errors.Add($"{name} is invalid");
        }
    }

    private static void ValidateColors(LpColorTokens colors, List<string> errors)
    {
        ValidateColor(colors.Primary, "colors.primary", errors);
        ValidateColor(colors.Accent, "colors.accent", errors);
        ValidateColor(colors.Bg, "colors.bg", errors);
        ValidateColor(colors.Text, "colors.text", errors);
        ValidateColor(colors.Muted, "colors.muted", errors);
        ValidateColor(colors.Border, "colors.border", errors);

        var families = GetHueFamilies(new[]
        {
            colors.Primary,
            colors.Accent,
            colors.Bg,
            colors.Text,
            colors.Muted,
            colors.Border
        });

        if (families > 3)
        {
            errors.Add("colors must be at most 3 hue families (+ gray allowed)");
        }
    }

    private static void ValidateTypography(LpTypographyTokens typography, List<string> errors)
    {
        if (typography.H1 < 20 || typography.H1 > 48)
        {
            errors.Add("typography.h1 out of range");
        }
        if (typography.H2 < 16 || typography.H2 > 36)
        {
            errors.Add("typography.h2 out of range");
        }
        if (typography.Body < 12 || typography.Body > 22)
        {
            errors.Add("typography.body out of range");
        }
        if (typography.Small < 10 || typography.Small > 18)
        {
            errors.Add("typography.small out of range");
        }
        if (!WeightScales.Contains(typography.WeightScale ?? string.Empty))
        {
            errors.Add("typography.weightScale is invalid");
        }
    }

    private static void ValidateSpacing(LpSpacingTokens spacing, List<string> errors)
    {
        if (!SpacingScale.Contains(spacing.SectionY))
        {
            errors.Add("spacing.sectionY must be 8px scale");
        }
        if (!SpacingScale.Contains(spacing.CardPadding))
        {
            errors.Add("spacing.cardPadding must be 8px scale");
        }
        if (!SpacingScale.Contains(spacing.GridGap))
        {
            errors.Add("spacing.gridGap must be 8px scale");
        }
    }

    private static void ValidateRadius(LpRadiusTokens radius, List<string> errors)
    {
        if (radius.Card < 0 || radius.Card > 32)
        {
            errors.Add("radius.card out of range");
        }
        if (radius.Button < 0 || radius.Button > 999)
        {
            errors.Add("radius.button out of range");
        }
        if (radius.Badge < 0 || radius.Badge > 999)
        {
            errors.Add("radius.badge out of range");
        }
    }

    private static void ValidateShadow(LpShadowTokens shadow, List<string> errors)
    {
        if (!string.Equals(shadow.Card, "soft", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("shadow.card must be soft");
        }
        if (!string.Equals(shadow.Sticky, "soft", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("shadow.sticky must be soft");
        }
    }

    private static void ValidateColor(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || !HexColor.IsMatch(value))
        {
            errors.Add($"{name} must be #RRGGBB");
        }
    }

    private static int GetHueFamilies(IEnumerable<string> colors)
    {
        var buckets = new HashSet<int>();
        foreach (var color in colors)
        {
            if (!TryParseRgb(color, out var r, out var g, out var b))
            {
                continue;
            }

            if (IsGray(r, g, b))
            {
                continue;
            }

            var hue = RgbToHue(r, g, b);
            var bucket = (int)Math.Round(hue / 30d);
            buckets.Add(bucket);
        }

        return buckets.Count;
    }

    private static bool TryParseRgb(string? value, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith('#'))
        {
            return false;
        }

        var hex = value.TrimStart('#');
        if (hex.Length != 6)
        {
            return false;
        }

        return int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out r)
            && int.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out g)
            && int.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
    }

    private static bool IsGray(int r, int g, int b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return (max - min) < 12;
    }

    private static double RgbToHue(int r, int g, int b)
    {
        var rf = r / 255d;
        var gf = g / 255d;
        var bf = b / 255d;
        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;
        if (delta == 0)
        {
            return 0;
        }

        double hue;
        if (max == rf)
        {
            hue = (gf - bf) / delta % 6;
        }
        else if (max == gf)
        {
            hue = (bf - rf) / delta + 2;
        }
        else
        {
            hue = (rf - gf) / delta + 4;
        }

        hue *= 60;
        if (hue < 0)
        {
            hue += 360;
        }

        return hue;
    }

    public LpReferenceStyleSpec Normalize(LpReferenceStyleSpec spec, List<string> warnings)
    {
        spec.StyleTokens ??= new LpStyleTokens();
        spec.StyleTokens.Colors ??= new LpColorTokens();
        spec.StyleTokens.Typography ??= new LpTypographyTokens();
        spec.StyleTokens.Spacing ??= new LpSpacingTokens();
        spec.StyleTokens.Radius ??= new LpRadiusTokens();
        spec.StyleTokens.Shadow ??= new LpShadowTokens();
        spec.LayoutRecipe ??= new LpLayoutRecipe();
        spec.DecorSpec ??= new LpDecorSpec();

        if (!WeightScales.Contains(spec.StyleTokens.Typography.WeightScale ?? string.Empty))
        {
            warnings.Add("typography.weightScale default applied");
            spec.StyleTokens.Typography.WeightScale = "medium";
        }

        spec.StyleTokens.Spacing.SectionY = NormalizeSpacing(spec.StyleTokens.Spacing.SectionY, warnings, "spacing.sectionY");
        spec.StyleTokens.Spacing.CardPadding = NormalizeSpacing(spec.StyleTokens.Spacing.CardPadding, warnings, "spacing.cardPadding");
        spec.StyleTokens.Spacing.GridGap = NormalizeSpacing(spec.StyleTokens.Spacing.GridGap, warnings, "spacing.gridGap");

        spec.StyleTokens.Radius.Card = Math.Clamp(spec.StyleTokens.Radius.Card, 0, 32);
        spec.StyleTokens.Radius.Button = Math.Clamp(spec.StyleTokens.Radius.Button, 0, 999);
        spec.StyleTokens.Radius.Badge = Math.Clamp(spec.StyleTokens.Radius.Badge, 0, 999);

        spec.StyleTokens.Shadow.Card = "soft";
        spec.StyleTokens.Shadow.Sticky = "soft";

        return spec;
    }

    private static int NormalizeSpacing(int value, List<string> warnings, string name)
    {
        if (SpacingScale.Contains(value))
        {
            return value;
        }

        warnings.Add($"{name} default applied");
        return 24;
    }
}

public class AiReferenceValidationResult
{
    public AiReferenceValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings, LpReferenceStyleSpec? normalizedSpec)
    {
        Errors = errors?.ToList() ?? new List<string>();
        Warnings = warnings?.ToList() ?? new List<string>();
        NormalizedSpec = normalizedSpec;
    }

    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public LpReferenceStyleSpec? NormalizedSpec { get; }

    public bool IsValid => Errors.Count == 0;
}
