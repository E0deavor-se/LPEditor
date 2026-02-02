using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class SectionStyleModel
{
    [JsonPropertyName("design")]
    public SectionDesignModel Design { get; set; } = new();

    [JsonPropertyName("layout")]
    public SectionLayoutSettings Layout { get; set; } = new();

    [JsonPropertyName("typography")]
    public SectionTypographySettings Typography { get; set; } = new();

    // TODO: セクション個別で共通カードテーマを上書きする拡張ポイント。
    [JsonPropertyName("frameStyleOverride")]
    public FrameStyle? FrameStyleOverride { get; set; }

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

public class SectionDesignModel
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "simple";

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; } = "#ffffff";

    [JsonPropertyName("backgroundOpacity")]
    public double? BackgroundOpacity { get; set; } = 1;

    [JsonPropertyName("gradientColorA")]
    public string? GradientColorA { get; set; }

    [JsonPropertyName("gradientColorB")]
    public string? GradientColorB { get; set; }

    [JsonPropertyName("gradientDirection")]
    public int? GradientDirection { get; set; } = 135;

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("overlayColor")]
    public string? OverlayColor { get; set; } = "#111827";

    [JsonPropertyName("overlayOpacity")]
    public double? OverlayOpacity { get; set; } = 0.35;

    [JsonPropertyName("borderRadius")]
    public int? BorderRadius { get; set; } = 16;

    [JsonPropertyName("paddingX")]
    public int? PaddingX { get; set; } = 24;

    [JsonPropertyName("paddingY")]
    public int? PaddingY { get; set; } = 24;

    [JsonPropertyName("paddingPreset")]
    public string? PaddingPreset { get; set; } = "md";

    [JsonPropertyName("marginBottom")]
    public int? MarginBottom { get; set; } = 16;

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; } = "#e5e7eb";

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; } = 1;

    [JsonPropertyName("shadowLevel")]
    public string? ShadowLevel { get; set; } = "md";

    [JsonPropertyName("accentLineEnabled")]
    public bool AccentLineEnabled { get; set; }

    [JsonPropertyName("accentColor")]
    public string? AccentColor { get; set; } = "#4f46e5";

    [JsonPropertyName("accentHeight")]
    public int? AccentHeight { get; set; } = 4;

    [JsonPropertyName("patternType")]
    public string? PatternType { get; set; } = "off";

    [JsonPropertyName("animation")]
    public string? Animation { get; set; } = "off";
}

public class SectionLayoutSettings
{
    [JsonPropertyName("paddingTop")]
    public int? PaddingTop { get; set; }

    [JsonPropertyName("paddingRight")]
    public int? PaddingRight { get; set; }

    [JsonPropertyName("paddingBottom")]
    public int? PaddingBottom { get; set; }

    [JsonPropertyName("paddingLeft")]
    public int? PaddingLeft { get; set; }

    [JsonPropertyName("gap")]
    public int? Gap { get; set; }

    [JsonPropertyName("maxWidth")]
    public int? MaxWidth { get; set; }
}

public class SectionTypographySettings
{
    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("fontWeight")]
    public int? FontWeight { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("align")]
    public string? Align { get; set; }
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

    [JsonPropertyName("intensity")]
    public int? Intensity { get; set; }

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

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("imageAlt")]
    public string ImageAlt { get; set; } = string.Empty;

    [JsonPropertyName("imageAutoFit")]
    public bool ImageAutoFit { get; set; } = true;

    [JsonPropertyName("applyToAll")]
    public bool ApplyToAll { get; set; }

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
