using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public sealed class FramePreset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("apply")]
    public FramePresetApply Apply { get; set; } = new();

    [JsonPropertyName("preview")]
    public FramePresetPreview Preview { get; set; } = new();
}

public sealed class FramePresetApply
{
    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("backgroundOpacity")]
    public int? BackgroundOpacity { get; set; }

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; }

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; }

    [JsonPropertyName("borderStyle")]
    public string? BorderStyle { get; set; }

    [JsonPropertyName("borderRadius")]
    public int? BorderRadius { get; set; }

    [JsonPropertyName("shadowLevel")]
    public string? ShadowLevel { get; set; }

    [JsonPropertyName("paddingPreset")]
    public string? PaddingPreset { get; set; }

    [JsonPropertyName("paddingX")]
    public int? PaddingX { get; set; }

    [JsonPropertyName("paddingY")]
    public int? PaddingY { get; set; }

    [JsonPropertyName("borderGradientColorA")]
    public string? BorderGradientColorA { get; set; }

    [JsonPropertyName("borderGradientColorB")]
    public string? BorderGradientColorB { get; set; }

    [JsonPropertyName("borderGradientAngle")]
    public int? BorderGradientAngle { get; set; }
}

public sealed class FramePresetPreview
{
    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("backgroundOpacity")]
    public int? BackgroundOpacity { get; set; }

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; }

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; }

    [JsonPropertyName("borderStyle")]
    public string? BorderStyle { get; set; }

    [JsonPropertyName("borderRadius")]
    public int? BorderRadius { get; set; }

    [JsonPropertyName("shadowLevel")]
    public string? ShadowLevel { get; set; }

    [JsonPropertyName("borderGradientColorA")]
    public string? BorderGradientColorA { get; set; }

    [JsonPropertyName("borderGradientColorB")]
    public string? BorderGradientColorB { get; set; }

    [JsonPropertyName("borderGradientAngle")]
    public int? BorderGradientAngle { get; set; }
}
