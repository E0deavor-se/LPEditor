namespace LPEditorApp.Models;

public sealed class BackgroundSetting
{
    public string SourceType { get; set; } = "solid";

    public string Mode { get; set; } = "color";

    public string Color { get; set; } = "#ffffff";

    public double? ColorOpacity { get; set; } = 1;

    public string GradientType { get; set; } = "linear";

    public double? GradientAngle { get; set; } = 135;

    public string GradientColorA { get; set; } = "#ffffff";

    public string GradientColorB { get; set; } = "#0f172a";

    public double? GradientOpacity { get; set; } = 1;

    public string ImageUrl { get; set; } = string.Empty;

    public string VideoUrl { get; set; } = string.Empty;

    public string VideoPoster { get; set; } = string.Empty;

    public string Repeat { get; set; } = "no-repeat";

    public string Position { get; set; } = "center center";

    public string PositionCustom { get; set; } = string.Empty;

    public string Size { get; set; } = "cover";

    public string SizeCustom { get; set; } = string.Empty;

    public string Attachment { get; set; } = "scroll";

    public BackgroundPresetSelection Preset { get; set; } = new();

    public BackgroundEffects Effects { get; set; } = new();
}
