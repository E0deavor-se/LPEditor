using System.Text.Json.Serialization;

namespace LPEditorApp.Models.Ai;

public class LpBlueprint
{
    [JsonPropertyName("meta")]
    public LpBlueprintMeta Meta { get; set; } = new();

    [JsonPropertyName("sections")]
    public List<LpBlueprintSection> Sections { get; set; } = new();
}

public class LpBlueprintMeta
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "ja";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tone")]
    public string Tone { get; set; } = "casual";

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = "acquisition";

    [JsonPropertyName("industry")]
    public string Industry { get; set; } = string.Empty;

    [JsonPropertyName("brand")]
    public LpBlueprintBrand Brand { get; set; } = new();
}

public class LpBlueprintBrand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("colorHint")]
    public string? ColorHint { get; set; }
}

public class LpBlueprintSection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("props")]
    public LpBlueprintSectionProps Props { get; set; } = new();
}

public class LpBlueprintSectionProps
{
    [JsonPropertyName("heading")]
    public string? Heading { get; set; }

    [JsonPropertyName("subheading")]
    public string? Subheading { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("bullets")]
    public List<string> Bullets { get; set; } = new();

    [JsonPropertyName("ctaText")]
    public string? CtaText { get; set; }

    [JsonPropertyName("disclaimer")]
    public string? Disclaimer { get; set; }

    [JsonPropertyName("items")]
    public List<LpBlueprintItem> Items { get; set; } = new();
}

public class LpBlueprintItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("badge")]
    public string? Badge { get; set; }
}
