using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public sealed class BackgroundEffects
{
    [JsonPropertyName("hue")]
    public double? Hue { get; set; }

    [JsonPropertyName("saturation")]
    public double? Saturation { get; set; }

    [JsonPropertyName("brightness")]
    public double? Brightness { get; set; }

    [JsonPropertyName("contrast")]
    public double? Contrast { get; set; }

    [JsonPropertyName("blur")]
    public double? Blur { get; set; }

    [JsonPropertyName("overlay")]
    public BackgroundOverlay Overlay { get; set; } = new();
}

public sealed class BackgroundOverlay
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#000000";

    [JsonPropertyName("opacity")]
    public double? Opacity { get; set; }

    [JsonPropertyName("blendMode")]
    public string BlendMode { get; set; } = "normal";
}
