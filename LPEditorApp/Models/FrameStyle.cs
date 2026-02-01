using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class FrameStyle
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "simple";

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; } = "#ffffff";

    [JsonPropertyName("backgroundOpacity")]
    public int? BackgroundOpacity { get; set; } = 100;

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; } = "#dc2626";

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; } = 6;

    [JsonPropertyName("borderStyle")]
    public string? BorderStyle { get; set; } = "solid";

    [JsonPropertyName("borderGradientColorA")]
    public string? BorderGradientColorA { get; set; }

    [JsonPropertyName("borderGradientColorB")]
    public string? BorderGradientColorB { get; set; }

    [JsonPropertyName("borderGradientAngle")]
    public int? BorderGradientAngle { get; set; }

    [JsonPropertyName("borderRadius")]
    public int? BorderRadius { get; set; } = 0;

    [JsonPropertyName("shadowLevel")]
    public string? ShadowLevel { get; set; } = "off";

    [JsonPropertyName("paddingPreset")]
    public string? PaddingPreset { get; set; } = "normal";

    [JsonPropertyName("paddingX")]
    public int? PaddingX { get; set; } = 24;

    [JsonPropertyName("paddingY")]
    public int? PaddingY { get; set; } = 20;

    [JsonPropertyName("maxWidthPx")]
    public int? MaxWidthPx { get; set; } = 900;

    [JsonPropertyName("centered")]
    public bool Centered { get; set; } = true;

    [JsonPropertyName("headerBackgroundColor")]
    public string? HeaderBackgroundColor { get; set; } = "#dc2626";

    [JsonPropertyName("headerTextColor")]
    public string? HeaderTextColor { get; set; } = "#ffffff";

    [JsonPropertyName("headerFontSizePx")]
    public int? HeaderFontSizePx { get; set; } = 30;

    [JsonPropertyName("headerFontFamily")]
    public string? HeaderFontFamily { get; set; }

    [JsonPropertyName("headerHeightPx")]
    public int? HeaderHeightPx { get; set; } = 80;

    [JsonPropertyName("headerRadiusTop")]
    public bool HeaderRadiusTop { get; set; } = true;

    [JsonPropertyName("bodyFontSizePx")]
    public int? BodyFontSizePx { get; set; } = 18;

    [JsonPropertyName("bodyFontFamily")]
    public string? BodyFontFamily { get; set; }

    [JsonPropertyName("cornerDecoration")]
    public CornerDecorationSet? CornerDecoration { get; set; }

    [JsonPropertyName("presetKey")]
    public string? PresetKey { get; set; }

    [JsonPropertyName("animationTargets")]
    public Dictionary<string, FrameAnimationTargetSetting> AnimationTargets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CornerDecorationSet
{
    [JsonPropertyName("topLeft")]
    public CornerDecoration? TopLeft { get; set; }

    [JsonPropertyName("topRight")]
    public CornerDecoration? TopRight { get; set; }

    [JsonPropertyName("bottomLeft")]
    public CornerDecoration? BottomLeft { get; set; }

    [JsonPropertyName("bottomRight")]
    public CornerDecoration? BottomRight { get; set; }
}

public class CornerDecoration
{
    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("sizePx")]
    public int? SizePx { get; set; }

    [JsonPropertyName("offsetX")]
    public int? OffsetX { get; set; }

    [JsonPropertyName("offsetY")]
    public int? OffsetY { get; set; }

    [JsonPropertyName("rotateDeg")]
    public double? RotateDeg { get; set; }

    [JsonPropertyName("opacity")]
    public int? Opacity { get; set; }

    [JsonPropertyName("flipX")]
    public bool? FlipX { get; set; }

    [JsonPropertyName("flipY")]
    public bool? FlipY { get; set; }

    [JsonPropertyName("zIndex")]
    public int? ZIndex { get; set; }

    [JsonPropertyName("inside")]
    public bool? Inside { get; set; }
}
