using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class PageEffectsSetting
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("sparkle")]
    public SparkleEffectSetting Sparkle { get; set; } = new();

    [JsonPropertyName("noise")]
    public NoiseEffectSetting Noise { get; set; } = new();

    [JsonPropertyName("gradientDrift")]
    public GradientDriftEffectSetting GradientDrift { get; set; } = new();
}

public class SparkleEffectSetting
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("intensity")]
    public string Intensity { get; set; } = "low";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#ffffff";

    [JsonPropertyName("density")]
    public int? Density { get; set; }

    [JsonPropertyName("speed")]
    public int? Speed { get; set; }
}

public class NoiseEffectSetting
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("intensity")]
    public string Intensity { get; set; } = "low";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#ffffff";

    [JsonPropertyName("density")]
    public int? Density { get; set; }
}

public class GradientDriftEffectSetting
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("intensity")]
    public string Intensity { get; set; } = "low";

    [JsonPropertyName("colorA")]
    public string ColorA { get; set; } = "#7c3aed";

    [JsonPropertyName("colorB")]
    public string ColorB { get; set; } = "#0ea5e9";

    [JsonPropertyName("speed")]
    public int? Speed { get; set; }
}
