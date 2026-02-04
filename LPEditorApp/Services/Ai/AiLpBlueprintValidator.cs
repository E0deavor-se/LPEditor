using System.Linq;
using System.Text.Json;
using LPEditorApp.Models.Ai;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public class AiLpBlueprintValidator
{
    private static readonly HashSet<string> AllowedSectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "hero", "benefits", "howto", "offer", "ranking", "faq", "notes", "footer"
    };

    private static readonly HashSet<string> AllowedTone = new(StringComparer.OrdinalIgnoreCase)
    {
        "casual", "formal"
    };

    private static readonly HashSet<string> AllowedGoal = new(StringComparer.OrdinalIgnoreCase)
    {
        "acquisition", "activation", "retention", "revenue"
    };

    private readonly AiOptions _options;

    private const int HeadingLimit = 40;
    private const int SubheadingLimit = 80;
    private const int BulletLimit = 60;
    private const int BodyLimit = 500;
    private const int ItemTitleLimit = 60;
    private const int ItemTextLimit = 200;
    private const int ItemBadgeLimit = 30;
    private const int MaxHardStringLength = 20000;
    private const int MaxSectionsHard = 50;

    public AiLpBlueprintValidator(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public AiLpBlueprintValidator() : this(Microsoft.Extensions.Options.Options.Create(new AiOptions()))
    {
    }

    public ValidationResult Validate(LpBlueprint? blueprint, JsonElement? raw = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (raw.HasValue)
        {
            ValidateNoUnknownFields(raw.Value, errors, warnings);
        }

        if (blueprint is null)
        {
            errors.Add("blueprint is null");
            return new ValidationResult(errors, warnings, null);
        }

        if (blueprint.Meta is null)
        {
            errors.Add("meta is required");
        }
        else
        {
            if (!string.Equals(blueprint.Meta.Language, "ja", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("meta.language must be 'ja'");
            }

            if (!AllowedTone.Contains(blueprint.Meta.Tone ?? string.Empty))
            {
                warnings.Add("meta.tone should be casual or formal");
            }

            if (!AllowedGoal.Contains(blueprint.Meta.Goal ?? string.Empty))
            {
                warnings.Add("meta.goal should be acquisition, activation, retention, or revenue");
            }

            if (blueprint.Meta.Brand is null)
            {
                errors.Add("meta.brand is required");
            }

            if (IsTooLarge(blueprint.Meta.Title) || IsTooLarge(blueprint.Meta.Industry) || IsTooLarge(blueprint.Meta.Brand?.Name))
            {
                errors.Add("meta fields too large");
            }
        }

        if (blueprint.Sections is null || blueprint.Sections.Count < 4)
        {
            errors.Add("sections must be at least 4");
        }

        var sections = blueprint.Sections ?? new List<LpBlueprintSection>();
        if (sections.Count > MaxSectionsHard)
        {
            errors.Add("sections too many");
        }

        var types = sections.Select(section => section.Type ?? string.Empty).ToList();
        var required = new[] { "hero", "offer", "howto", "notes", "footer" };
        foreach (var req in required)
        {
            if (!types.Any(type => string.Equals(type, req, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"section '{req}' is required");
            }
        }

        foreach (var section in sections)
        {
            if (!AllowedSectionTypes.Contains(section.Type ?? string.Empty))
            {
                errors.Add($"unknown section type: {section.Type}");
            }

            if (string.IsNullOrWhiteSpace(section.Id))
            {
                errors.Add("section.id is required");
            }

            if (section.Props is null)
            {
                errors.Add($"section.props is required: {section.Id}");
                continue;
            }

            if (IsTooLarge(section.Props.Heading)
                || IsTooLarge(section.Props.Subheading)
                || IsTooLarge(section.Props.Body)
                || IsTooLarge(section.Props.CtaText)
                || IsTooLarge(section.Props.Disclaimer))
            {
                errors.Add($"props too large: {section.Id}");
            }

            if (section.Props.Bullets is null)
            {
                errors.Add($"bullets is required: {section.Id}");
            }
            else
            {
                foreach (var bullet in section.Props.Bullets)
                {
                    if (IsTooLarge(bullet))
                    {
                        errors.Add($"bullet too large: {section.Id}");
                    }
                }
            }

            if (section.Props.Items is null)
            {
                errors.Add($"items is required: {section.Id}");
            }
            else
            {
                foreach (var item in section.Props.Items)
                {
                    if (IsTooLarge(item.Title) || IsTooLarge(item.Text) || IsTooLarge(item.Badge))
                    {
                        errors.Add($"item too large: {section.Id}");
                    }
                }
            }
        }

        if (errors.Count == 0 && blueprint is not null)
        {
            var normalized = Normalize(blueprint, warnings);
            return new ValidationResult(errors, warnings, normalized);
        }

        return new ValidationResult(errors, warnings, null);
    }

    public (LpBlueprint? Blueprint, ValidationResult Result) TryParseAndValidate(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var blueprint = doc.RootElement.Deserialize<LpBlueprint>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = Validate(blueprint, doc.RootElement);
            return (blueprint, result);
        }
        catch (Exception ex)
        {
            var result = new ValidationResult(new List<string> { $"json parse failed: {ex.Message}" }, new List<string>(), null);
            return (null, result);
        }
    }

    private void ValidateNoUnknownFields(JsonElement root, List<string> errors, List<string> warnings)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("root must be an object");
            return;
        }

        var rootProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "meta", "sections" };
        foreach (var prop in root.EnumerateObject())
        {
            if (!rootProps.Contains(prop.Name))
            {
                AddUnknownFieldError($"unknown root field: {prop.Name}", errors, warnings);
            }
        }

        if (root.TryGetProperty("meta", out var meta))
        {
            if (meta.ValueKind != JsonValueKind.Object)
            {
                errors.Add("meta must be object");
            }
            else
            {
                var metaProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "language", "title", "tone", "goal", "industry", "brand"
                };
                foreach (var prop in meta.EnumerateObject())
                {
                    if (!metaProps.Contains(prop.Name))
                    {
                        AddUnknownFieldError($"unknown meta field: {prop.Name}", errors, warnings);
                    }
                }

                if (meta.TryGetProperty("brand", out var brand))
                {
                    if (brand.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add("meta.brand must be object");
                    }
                    else
                    {
                        var brandProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "name", "colorHint"
                        };
                        foreach (var prop in brand.EnumerateObject())
                        {
                            if (!brandProps.Contains(prop.Name))
                            {
                                AddUnknownFieldError($"unknown brand field: {prop.Name}", errors, warnings);
                            }
                        }
                    }
                }
            }
        }

        if (root.TryGetProperty("sections", out var sections))
        {
            if (sections.ValueKind != JsonValueKind.Array)
            {
                errors.Add("sections must be array");
            }
            else
            {
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add("section must be object");
                        continue;
                    }

                    var sectionProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "type", "id", "props"
                    };
                    foreach (var prop in section.EnumerateObject())
                    {
                        if (!sectionProps.Contains(prop.Name))
                        {
                            AddUnknownFieldError($"unknown section field: {prop.Name}", errors, warnings);
                        }
                    }

                    if (section.TryGetProperty("props", out var props))
                    {
                        if (props.ValueKind != JsonValueKind.Object)
                        {
                            errors.Add("props must be object");
                            continue;
                        }

                        var propsFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "heading", "subheading", "body", "bullets", "ctaText", "disclaimer", "items"
                        };
                        foreach (var prop in props.EnumerateObject())
                        {
                            if (!propsFields.Contains(prop.Name))
                            {
                                AddUnknownFieldError($"unknown props field: {prop.Name}", errors, warnings);
                            }
                        }

                        if (props.TryGetProperty("bullets", out var bullets) && bullets.ValueKind != JsonValueKind.Array && bullets.ValueKind != JsonValueKind.Null)
                        {
                            errors.Add("props.bullets must be array");
                        }
                        if (props.TryGetProperty("items", out var items) && items.ValueKind != JsonValueKind.Array && items.ValueKind != JsonValueKind.Null)
                        {
                            errors.Add("props.items must be array");
                        }

                        if (props.TryGetProperty("items", out var itemArray) && itemArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in itemArray.EnumerateArray())
                            {
                                if (item.ValueKind != JsonValueKind.Object)
                                {
                                    errors.Add("props.items item must be object");
                                    continue;
                                }

                                var itemFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    "title", "text", "badge"
                                };
                                foreach (var prop in item.EnumerateObject())
                                {
                                    if (!itemFields.Contains(prop.Name))
                                    {
                                        AddUnknownFieldError($"unknown item field: {prop.Name}", errors, warnings);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void AddUnknownFieldError(string message, List<string> errors, List<string> warnings)
    {
        if (_options.StrictJsonOnly)
        {
            errors.Add(message);
            return;
        }

        warnings.Add(message);
    }

    private static bool IsTooLarge(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length > MaxHardStringLength;
    }

    private static string? TruncateSoft(string? value, int limit, List<string> warnings, string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            warnings.Add($"{fieldKey} is empty");
            return value;
        }

        if (value.Length <= limit)
        {
            return value;
        }

        warnings.Add($"{fieldKey} trimmed to {limit}");
        return value[..limit];
    }

    private static List<string> EnsureBullets(List<string> bullets, int min, int max, List<string> warnings, string fieldKey)
    {
        var cleaned = bullets
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text.Trim())
            .ToList();

        if (cleaned.Count < min)
        {
            warnings.Add($"{fieldKey} bullets不足 (min {min})");
            var fallback = new List<string>
            {
                "内容は予告なく変更となる場合があります。",
                "詳細は公式サイトでご確認ください。",
                "最新情報は店舗・窓口でご確認ください。"
            };
            while (cleaned.Count < min && fallback.Count > 0)
            {
                cleaned.Add(fallback[0]);
                fallback.RemoveAt(0);
            }
        }

        if (cleaned.Count > max)
        {
            warnings.Add($"{fieldKey} bullets多すぎ (max {max})");
            cleaned = cleaned.Take(max).ToList();
        }

        return cleaned;
    }

    public LpBlueprint Normalize(LpBlueprint blueprint, List<string> warnings)
    {
        foreach (var section in blueprint.Sections)
        {
            if (section.Props is null)
            {
                continue;
            }

            section.Props.Heading = TruncateSoft(section.Props.Heading, HeadingLimit, warnings, $"{section.Id}.heading");
            section.Props.Subheading = TruncateSoft(section.Props.Subheading, SubheadingLimit, warnings, $"{section.Id}.subheading");
            section.Props.Body = TruncateSoft(section.Props.Body, BodyLimit, warnings, $"{section.Id}.body");
            section.Props.CtaText = TruncateSoft(section.Props.CtaText, 40, warnings, $"{section.Id}.ctaText");
            section.Props.Disclaimer = TruncateSoft(section.Props.Disclaimer, 120, warnings, $"{section.Id}.disclaimer");

            if (section.Props.Bullets is not null)
            {
                section.Props.Bullets = EnsureBullets(section.Props.Bullets, 3, 10, warnings, $"{section.Id}.bullets")
                    .Select(text => TruncateSoft(text, BulletLimit, warnings, $"{section.Id}.bullet") ?? string.Empty)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToList();
            }

            if (section.Props.Items is not null)
            {
                if (section.Props.Items.Count == 0)
                {
                    warnings.Add($"{section.Id}.items is empty");
                }
                foreach (var item in section.Props.Items)
                {
                    item.Title = TruncateSoft(item.Title, ItemTitleLimit, warnings, $"{section.Id}.item.title") ?? string.Empty;
                    item.Text = TruncateSoft(item.Text, ItemTextLimit, warnings, $"{section.Id}.item.text") ?? string.Empty;
                    item.Badge = TruncateSoft(item.Badge, ItemBadgeLimit, warnings, $"{section.Id}.item.badge");
                }
            }

            if (string.Equals(section.Type, "notes", StringComparison.OrdinalIgnoreCase) && section.Props.Bullets is not null)
            {
                section.Props.Bullets = EnsureBullets(section.Props.Bullets, 5, 10, warnings, $"{section.Id}.notes");
            }
        }

        return blueprint;
    }
}

public class ValidationResult
{
    public ValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings, LpBlueprint? normalizedBlueprint)
    {
        Errors = errors?.ToList() ?? new List<string>();
        Warnings = warnings?.ToList() ?? new List<string>();
        NormalizedBlueprint = normalizedBlueprint;
    }

    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public LpBlueprint? NormalizedBlueprint { get; }

    public bool IsValid => Errors.Count == 0;
}
