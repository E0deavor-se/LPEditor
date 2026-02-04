using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class BackgroundPresetSelection
{
    [JsonPropertyName("presetKey")]
    public string PresetKey { get; set; } = string.Empty;

    [JsonPropertyName("cssClass")]
    public string CssClass { get; set; } = string.Empty;

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

    [JsonPropertyName("scale")]
    public double? Scale { get; set; }

    [JsonPropertyName("blur")]
    public double? Blur { get; set; }
}
