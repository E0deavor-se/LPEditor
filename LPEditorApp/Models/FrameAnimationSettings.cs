using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public sealed class FrameAnimationTargetSetting
{
    [JsonPropertyName("presetId")]
    public string PresetId { get; set; } = "none";

    [JsonPropertyName("durationMs")]
    public int? DurationMs { get; set; }

    [JsonPropertyName("delayMs")]
    public int? DelayMs { get; set; }

    [JsonPropertyName("easing")]
    public string? Easing { get; set; }

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = "scroll";

    [JsonPropertyName("loop")]
    public bool Loop { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("spDurationRate")]
    public double? SpDurationRate { get; set; }
}
