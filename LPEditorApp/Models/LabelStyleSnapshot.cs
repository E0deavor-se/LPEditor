namespace LPEditorApp.Models;

public sealed class LabelStyleSnapshot
{
    public string BackgroundColor { get; set; } = string.Empty;
    public double? BackgroundOpacity { get; set; }
    public string BorderColor { get; set; } = string.Empty;
    public double? BorderOpacity { get; set; }
    public int? BorderWidth { get; set; }
    public string TextColor { get; set; } = string.Empty;
    public double? TextOpacity { get; set; }
    public int? FontSize { get; set; }
    public bool? FontBold { get; set; }
}
