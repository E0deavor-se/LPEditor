using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class BackgroundPresetModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("cssClass")]
    public string CssClass { get; set; } = string.Empty;

    [JsonPropertyName("defaults")]
    public BackgroundPresetDefaults Defaults { get; set; } = new();

    [JsonPropertyName("recommendedUse")]
    public string RecommendedUse { get; set; } = string.Empty;
}

public class BackgroundPresetDefaults
{
    [JsonPropertyName("base")]
    public string BaseColor { get; set; } = "#f8fafc";

    [JsonPropertyName("accent")]
    public string AccentColor { get; set; } = "#cbd5f5";

    [JsonPropertyName("opacity")]
    public double? Opacity { get; set; }

    [JsonPropertyName("scale")]
    public double? Scale { get; set; }
}
