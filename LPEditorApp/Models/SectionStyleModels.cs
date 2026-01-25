using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class SectionStyleModel
{
    [JsonPropertyName("background")]
    public SectionBackgroundSettings Background { get; set; } = new();

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; set; } = string.Empty;

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; }

    [JsonPropertyName("borderStyle")]
    public string BorderStyle { get; set; } = "solid";

    [JsonPropertyName("radius")]
    public int? Radius { get; set; }

    [JsonPropertyName("shadow")]
    public string Shadow { get; set; } = string.Empty;

    [JsonPropertyName("paddingTop")]
    public int? PaddingTop { get; set; }

    [JsonPropertyName("paddingRight")]
    public int? PaddingRight { get; set; }

    [JsonPropertyName("paddingBottom")]
    public int? PaddingBottom { get; set; }

    [JsonPropertyName("paddingLeft")]
    public int? PaddingLeft { get; set; }

    [JsonPropertyName("divider")]
    public SectionDividerModel Divider { get; set; } = new();

    [JsonPropertyName("decorations")]
    public List<DecorationLayer> Decorations { get; set; } = new();

    [JsonPropertyName("sectionAnimation")]
    public SectionAnimationModel SectionAnimation { get; set; } = new();
}

public class SectionDividerModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("thickness")]
    public int? Thickness { get; set; }

    [JsonPropertyName("style")]
    public string Style { get; set; } = "solid";

    [JsonPropertyName("marginTop")]
    public int? MarginTop { get; set; }

    [JsonPropertyName("marginBottom")]
    public int? MarginBottom { get; set; }
}

public class SectionAnimationModel
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "none";

    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    [JsonPropertyName("delay")]
    public int? Delay { get; set; }

    [JsonPropertyName("easing")]
    public string Easing { get; set; } = "ease";

    [JsonPropertyName("once")]
    public bool Once { get; set; } = true;

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = "scroll";

    [JsonPropertyName("repeat")]
    public string Repeat { get; set; } = "none";
}

public class DecorationLayer
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "ribbon";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("layer")]
    public string Layer { get; set; } = "front";

    [JsonPropertyName("position")]
    public string Position { get; set; } = "top";

    [JsonPropertyName("offsetX")]
    public int? OffsetX { get; set; }

    [JsonPropertyName("offsetY")]
    public int? OffsetY { get; set; }

    [JsonPropertyName("sizePreset")]
    public string SizePreset { get; set; } = "m";

    [JsonPropertyName("sizePx")]
    public int? SizePx { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#1e1b4b";

    [JsonPropertyName("opacityPct")]
    public int? OpacityPct { get; set; } = 100;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("animation")]
    public DecorationAnimationSetting Animation { get; set; } = new();
}

public class DecorationAnimationSetting
{
    [JsonPropertyName("preset")]
    public string Preset { get; set; } = "none";

    [JsonPropertyName("durationMs")]
    public int? DurationMs { get; set; }

    [JsonPropertyName("delayMs")]
    public int? DelayMs { get; set; }

    [JsonPropertyName("easing")]
    public string Easing { get; set; } = "ease";

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = "scroll";

    [JsonPropertyName("repeat")]
    public string Repeat { get; set; } = "none";
}
