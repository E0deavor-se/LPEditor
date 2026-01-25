using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class BackgroundPresetModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("pageBackground")]
    public LpBackgroundModel PageBackground { get; set; } = new();

    [JsonPropertyName("sectionBackgrounds")]
    public Dictionary<string, SectionBackgroundSettings>? SectionBackgrounds { get; set; }
}
