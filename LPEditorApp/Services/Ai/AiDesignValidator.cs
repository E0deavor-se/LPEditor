using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using LPEditorApp.Models.Ai;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiDesignValidator
{
    private readonly AiOptions _options;

    private static readonly HashSet<string> DesignTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ranking", "municipality_rebate", "municipality_lottery", "coupon", "point_lottery"
    };

    private static readonly HashSet<string> ShadowTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "soft", "none"
    };

    private static readonly HashSet<string> FontTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "rounded", "gothic"
    };

    private static readonly HashSet<string> CtaStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "solid", "outline", "gradient"
    };

    private static readonly HashSet<string> ContainerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "centered", "wide"
    };

    private static readonly HashSet<string> HeroTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "split", "stacked", "poster"
    };

    private static readonly HashSet<string> SectionStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "card", "band", "flat"
    };

    private static readonly HashSet<string> HeadingStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "pill", "underline", "bold"
    };

    private static readonly HashSet<string> OfferStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "cardGrid", "singleCard"
    };

    private static readonly HashSet<string> HowtoStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "steps", "timeline"
    };

    private static readonly HashSet<string> NotesStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "accordion", "boxed"
    };

    private static readonly HashSet<string> RankingStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "table", "cards", "podium"
    };

    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public AiDesignValidator(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public AiDesignValidator() : this(Options.Create(new AiOptions()))
    {
    }

    public AiDesignValidationResult Validate(LpDesignSpec? spec, JsonElement? raw = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (raw.HasValue)
        {
            ValidateNoUnknownFields(raw.Value, errors, warnings);
        }

        if (spec is null)
        {
            errors.Add("design spec is null");
            return new AiDesignValidationResult(errors, warnings, null);
        }

        if (spec.Version != 1)
        {
            errors.Add("version must be 1");
        }

        if (!DesignTypes.Contains(spec.DesignType ?? string.Empty))
        {
            errors.Add("designType is invalid");
        }

        if (spec.Theme is null)
        {
            errors.Add("theme is required");
        }
        else
        {
            ValidateColor(spec.Theme.Primary, "theme.primary", errors);
            ValidateColor(spec.Theme.Secondary, "theme.secondary", errors);
            ValidateColor(spec.Theme.Accent, "theme.accent", errors);
            ValidateColor(spec.Theme.Bg, "theme.bg", errors);
            ValidateColor(spec.Theme.Text, "theme.text", errors);

            if (spec.Theme.Radius < 0 || spec.Theme.Radius > 32)
            {
                errors.Add("theme.radius out of range");
            }

            if (!ShadowTypes.Contains(spec.Theme.Shadow ?? string.Empty))
            {
                warnings.Add("theme.shadow default applied");
                spec.Theme.Shadow = "soft";
            }

            if (!FontTypes.Contains(spec.Theme.Font ?? string.Empty))
            {
                warnings.Add("theme.font default applied");
                spec.Theme.Font = "system";
            }

            if (!CtaStyles.Contains(spec.Theme.CtaStyle ?? string.Empty))
            {
                warnings.Add("theme.ctaStyle default applied");
                spec.Theme.CtaStyle = "solid";
            }
        }

        if (spec.Layout is null)
        {
            errors.Add("layout is required");
        }
        else
        {
            ValidateEnum(spec.Layout.Container, ContainerTypes, "layout.container", errors);
            ValidateEnum(spec.Layout.Hero, HeroTypes, "layout.hero", errors);
            ValidateEnum(spec.Layout.SectionStyle, SectionStyles, "layout.sectionStyle", errors);
            ValidateEnum(spec.Layout.HeadingStyle, HeadingStyles, "layout.headingStyle", errors);
            ValidateEnum(spec.Layout.OfferStyle, OfferStyles, "layout.offerStyle", errors);
            ValidateEnum(spec.Layout.HowtoStyle, HowtoStyles, "layout.howtoStyle", errors);
            ValidateEnum(spec.Layout.NotesStyle, NotesStyles, "layout.notesStyle", errors);
            ValidateEnum(spec.Layout.RankingStyle, RankingStyles, "layout.rankingStyle", errors);
        }

        if (errors.Count == 0)
        {
            var normalized = Normalize(spec, warnings);
            return new AiDesignValidationResult(errors, warnings, normalized);
        }

        return new AiDesignValidationResult(errors, warnings, null);
    }

    public (LpDesignSpec? Spec, AiDesignValidationResult Result) TryParseAndValidate(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var spec = doc.RootElement.Deserialize<LpDesignSpec>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = Validate(spec, doc.RootElement);
            return (spec, result);
        }
        catch (Exception ex)
        {
            var result = new AiDesignValidationResult(new List<string> { $"json parse failed: {ex.Message}" }, new List<string>(), null);
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

        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "version", "designType", "theme", "layout" };
        foreach (var prop in root.EnumerateObject())
        {
            if (!rootFields.Contains(prop.Name))
            {
                AddUnknownField($"unknown root field: {prop.Name}", errors, warnings);
            }
        }

        if (root.TryGetProperty("theme", out var theme) && theme.ValueKind == JsonValueKind.Object)
        {
            var themeFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "primary", "secondary", "accent", "bg", "text", "radius", "shadow", "font", "ctaStyle"
            };
            foreach (var prop in theme.EnumerateObject())
            {
                if (!themeFields.Contains(prop.Name))
                {
                    AddUnknownField($"unknown theme field: {prop.Name}", errors, warnings);
                }
            }
        }

        if (root.TryGetProperty("layout", out var layout) && layout.ValueKind == JsonValueKind.Object)
        {
            var layoutFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "container", "hero", "sectionStyle", "headingStyle", "offerStyle", "howtoStyle", "notesStyle", "rankingStyle"
            };
            foreach (var prop in layout.EnumerateObject())
            {
                if (!layoutFields.Contains(prop.Name))
                {
                    AddUnknownField($"unknown layout field: {prop.Name}", errors, warnings);
                }
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

    private static void ValidateColor(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || !HexColor.IsMatch(value))
        {
            errors.Add($"{name} must be #RRGGBB");
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

    public LpDesignSpec Normalize(LpDesignSpec spec, List<string> warnings)
    {
        spec.Theme.Primary = NormalizeColor(spec.Theme.Primary, warnings, "theme.primary");
        spec.Theme.Secondary = NormalizeColor(spec.Theme.Secondary, warnings, "theme.secondary");
        spec.Theme.Accent = NormalizeColor(spec.Theme.Accent, warnings, "theme.accent");
        spec.Theme.Bg = NormalizeColor(spec.Theme.Bg, warnings, "theme.bg");
        spec.Theme.Text = NormalizeColor(spec.Theme.Text, warnings, "theme.text");
        spec.Theme.Radius = Math.Clamp(spec.Theme.Radius, 0, 32);
        return spec;
    }
}

public class AiDesignValidationResult
{
    public AiDesignValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings, LpDesignSpec? normalizedSpec)
    {
        Errors = errors?.ToList() ?? new List<string>();
        Warnings = warnings?.ToList() ?? new List<string>();
        NormalizedSpec = normalizedSpec;
    }

    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public LpDesignSpec? NormalizedSpec { get; }

    public bool IsValid => Errors.Count == 0;
}
