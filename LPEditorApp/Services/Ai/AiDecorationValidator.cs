using System.Text.Json;
using System.Text.RegularExpressions;
using LPEditorApp.Models.Ai;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiDecorationValidator
{
    private readonly AiOptions _options;
    private const bool ForceStrictJsonOnly = true;

    private static readonly HashSet<string> BackgroundTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "solid", "gradient", "pattern"
    };

    private static readonly HashSet<string> PatternTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "dots", "waves", "none"
    };

    private static readonly HashSet<string> FrameStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "card", "flat", "band"
    };

    private static readonly HashSet<string> FrameShadows = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "soft", "medium"
    };

    private static readonly HashSet<string> FrameBorders = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "light"
    };

    private static readonly HashSet<string> HeadingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "accent-line", "pill", "label"
    };

    private static readonly HashSet<string> CtaStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "badge", "glow"
    };

    private static readonly HashSet<string> DividerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "wave", "zigzag"
    };

    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public AiDecorationValidator(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public AiDecorationValidator() : this(Options.Create(new AiOptions()))
    {
    }

    public AiDecorationValidationResult Validate(LpDecorationSpec? spec, JsonElement? raw = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (raw.HasValue)
        {
            ValidateNoUnknownFields(raw.Value, errors, warnings);
        }

        if (spec is null)
        {
            errors.Add("decoration spec is null");
            return new AiDecorationValidationResult(errors, warnings, null);
        }

        if (spec.Background is null)
        {
            errors.Add("background is required");
        }
        else
        {
            ValidateEnum(spec.Background.Type, BackgroundTypes, "background.type", errors);
            ValidateEnum(spec.Background.Pattern, PatternTypes, "background.pattern", errors);
            ValidateColorList(spec.Background.Colors, "background.colors", errors);
            if (spec.Background.Opacity < 0 || spec.Background.Opacity > 0.6)
            {
                errors.Add("background.opacity out of range");
            }
        }

        if (spec.SectionFrame is null)
        {
            errors.Add("sectionFrame is required");
        }
        else
        {
            ValidateEnum(spec.SectionFrame.Style, FrameStyles, "sectionFrame.style", errors);
            ValidateEnum(spec.SectionFrame.Shadow, FrameShadows, "sectionFrame.shadow", errors);
            ValidateEnum(spec.SectionFrame.Border, FrameBorders, "sectionFrame.border", errors);
            if (spec.SectionFrame.Radius < 0 || spec.SectionFrame.Radius > 28)
            {
                errors.Add("sectionFrame.radius out of range");
            }
        }

        if (spec.HeadingDecoration is null)
        {
            errors.Add("headingDecoration is required");
        }
        else
        {
            ValidateEnum(spec.HeadingDecoration.Type, HeadingTypes, "headingDecoration.type", errors);
            ValidateColor(spec.HeadingDecoration.Color, "headingDecoration.color", errors);
            if (spec.HeadingDecoration.Thickness < 0 || spec.HeadingDecoration.Thickness > 8)
            {
                errors.Add("headingDecoration.thickness out of range");
            }
        }

        if (spec.CtaEmphasis is null)
        {
            errors.Add("ctaEmphasis is required");
        }
        else
        {
            ValidateEnum(spec.CtaEmphasis.Style, CtaStyles, "ctaEmphasis.style", errors);
            ValidateColor(spec.CtaEmphasis.Color, "ctaEmphasis.color", errors);
        }

        if (spec.SectionDivider is null)
        {
            errors.Add("sectionDivider is required");
        }
        else
        {
            ValidateEnum(spec.SectionDivider.Type, DividerTypes, "sectionDivider.type", errors);
            ValidateColor(spec.SectionDivider.Color, "sectionDivider.color", errors);
            if (spec.SectionDivider.Height < 0 || spec.SectionDivider.Height > 48)
            {
                errors.Add("sectionDivider.height out of range");
            }
        }

        if (errors.Count == 0)
        {
            var normalized = Normalize(spec, warnings);
            return new AiDecorationValidationResult(errors, warnings, normalized);
        }

        return new AiDecorationValidationResult(errors, warnings, null);
    }

    public (LpDecorationSpec? Spec, AiDecorationValidationResult Result) TryParseAndValidate(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var spec = doc.RootElement.Deserialize<LpDecorationSpec>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = Validate(spec, doc.RootElement);
            return (spec, result);
        }
        catch (Exception ex)
        {
            var result = new AiDecorationValidationResult(new List<string> { $"json parse failed: {ex.Message}" }, new List<string>(), null);
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
            "background", "sectionFrame", "headingDecoration", "ctaEmphasis", "sectionDivider"
        };

        foreach (var prop in root.EnumerateObject())
        {
            if (!rootFields.Contains(prop.Name))
            {
                AddUnknownField($"unknown root field: {prop.Name}", errors, warnings);
            }
        }

        ValidateUnknownObject(root, "background", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "type", "colors", "pattern", "opacity" }, errors, warnings);
        ValidateUnknownObject(root, "sectionFrame", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "style", "radius", "shadow", "border" }, errors, warnings);
        ValidateUnknownObject(root, "headingDecoration", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "type", "color", "thickness" }, errors, warnings);
        ValidateUnknownObject(root, "ctaEmphasis", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "style", "color" }, errors, warnings);
        ValidateUnknownObject(root, "sectionDivider", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "type", "height", "color" }, errors, warnings);
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
    }

    private void AddUnknownField(string message, List<string> errors, List<string> warnings)
    {
        if (ForceStrictJsonOnly || _options.StrictJsonOnly)
        {
            errors.Add(message);
        }
        else
        {
            warnings.Add(message);
        }
    }

    private static void ValidateColor(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || !HexColor.IsMatch(value))
        {
            errors.Add($"{name} must be #RRGGBB");
        }
    }

    private static void ValidateColorList(List<string>? list, string name, List<string> errors)
    {
        if (list is null || list.Count == 0)
        {
            errors.Add($"{name} must have at least 1 color");
            return;
        }

        if (list.Count > 2)
        {
            errors.Add($"{name} must have at most 2 colors");
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (!HexColor.IsMatch(list[i]))
            {
                errors.Add($"{name}[{i}] must be #RRGGBB");
            }
        }
    }

    private static void ValidateEnum(string? value, HashSet<string> allowed, string name, List<string> errors)
    {
        if (!allowed.Contains(value ?? string.Empty))
        {
            errors.Add($"{name} is invalid");
        }
    }

    private static string NormalizeColor(string value, List<string> warnings, string name)
    {
        if (HexColor.IsMatch(value))
        {
            return value.ToUpperInvariant();
        }

        warnings.Add($"{name} color normalized");
        return "#000000";
    }

    public LpDecorationSpec Normalize(LpDecorationSpec spec, List<string> warnings)
    {
        spec.Background.Colors ??= new List<string>();
        if (spec.Background.Colors.Count == 0)
        {
            warnings.Add("background.colors default applied");
            spec.Background.Colors.Add("#F8FAFC");
        }

        if (spec.Background.Colors.Count == 1)
        {
            spec.Background.Colors.Add(spec.Background.Colors[0]);
        }

        spec.Background.Colors[0] = NormalizeColor(spec.Background.Colors[0], warnings, "background.colors[0]");
        spec.Background.Colors[1] = NormalizeColor(spec.Background.Colors[1], warnings, "background.colors[1]");
        spec.Background.Opacity = Math.Clamp(spec.Background.Opacity, 0, 0.6);

        spec.SectionFrame.Radius = Math.Clamp(spec.SectionFrame.Radius, 0, 28);
        spec.HeadingDecoration.Thickness = Math.Clamp(spec.HeadingDecoration.Thickness, 0, 8);
        spec.SectionDivider.Height = Math.Clamp(spec.SectionDivider.Height, 0, 48);

        spec.HeadingDecoration.Color = NormalizeColor(spec.HeadingDecoration.Color, warnings, "headingDecoration.color");
        spec.CtaEmphasis.Color = NormalizeColor(spec.CtaEmphasis.Color, warnings, "ctaEmphasis.color");
        spec.SectionDivider.Color = NormalizeColor(spec.SectionDivider.Color, warnings, "sectionDivider.color");

        return spec;
    }
}

public class AiDecorationValidationResult
{
    public AiDecorationValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings, LpDecorationSpec? normalizedSpec)
    {
        Errors = errors?.ToList() ?? new List<string>();
        Warnings = warnings?.ToList() ?? new List<string>();
        NormalizedSpec = normalizedSpec;
    }

    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public LpDecorationSpec? NormalizedSpec { get; }

    public bool IsValid => Errors.Count == 0;
}
