using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public sealed class AnimationPreset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("cssClass")]
    public string CssClass { get; set; } = string.Empty;

    [JsonPropertyName("defaultDurationMs")]
    public int DefaultDurationMs { get; set; } = 600;

    [JsonPropertyName("defaultDelayMs")]
    public int DefaultDelayMs { get; set; } = 0;

    [JsonPropertyName("defaultEasing")]
    public string DefaultEasing { get; set; } = "ease";

    [JsonPropertyName("recommendedUse")]
    public string RecommendedUse { get; set; } = string.Empty;

    [JsonPropertyName("spDurationRate")]
    public double? SpDurationRate { get; set; }
}
