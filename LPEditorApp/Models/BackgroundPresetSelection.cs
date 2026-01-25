using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class BackgroundPresetSelection
{
    [JsonPropertyName("presetKey")]
    public string PresetKey { get; set; } = string.Empty;

    [JsonPropertyName("angle")]
    public double? Angle { get; set; }

    [JsonPropertyName("colorA")]
    public string ColorA { get; set; } = string.Empty;

    [JsonPropertyName("colorB")]
    public string ColorB { get; set; } = string.Empty;

    [JsonPropertyName("opacity")]
    public double? Opacity { get; set; }

    [JsonPropertyName("density")]
    public double? Density { get; set; }
}
