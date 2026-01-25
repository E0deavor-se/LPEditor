namespace LPEditorApp.Models;

public static class LabelStylePresetCatalog
{
    public static IReadOnlyList<LabelStylePreset> Presets { get; } = new List<LabelStylePreset>
    {
        new("emphasis", "強調", "視認性の高い強調スタイル", new LabelStyleSnapshot
        {
            BackgroundColor = "#fee2e2",
            BackgroundOpacity = 1,
            BorderColor = "#fecaca",
            BorderOpacity = 1,
            BorderWidth = 1,
            TextColor = "#b91c1c",
            TextOpacity = 1,
            FontSize = 12,
            FontBold = true
        }),
        new("attention", "注意", "注意喚起に最適", new LabelStyleSnapshot
        {
            BackgroundColor = "#fff7ed",
            BackgroundOpacity = 1,
            BorderColor = "#fed7aa",
            BorderOpacity = 1,
            BorderWidth = 1,
            TextColor = "#c2410c",
            TextOpacity = 1,
            FontSize = 12,
            FontBold = true
        }),
        new("success", "成功", "肯定的な強調", new LabelStyleSnapshot
        {
            BackgroundColor = "#dcfce7",
            BackgroundOpacity = 1,
            BorderColor = "#86efac",
            BorderOpacity = 1,
            BorderWidth = 1,
            TextColor = "#15803d",
            TextOpacity = 1,
            FontSize = 12,
            FontBold = true
        }),
        new("disabled", "無効", "薄めのモノトーン", new LabelStyleSnapshot
        {
            BackgroundColor = "#e2e8f0",
            BackgroundOpacity = 0.7,
            BorderColor = "#cbd5f5",
            BorderOpacity = 0.7,
            BorderWidth = 1,
            TextColor = "#64748b",
            TextOpacity = 0.8,
            FontSize = 12,
            FontBold = false
        }),
        new("mono", "モノクロ", "落ち着いたトーン", new LabelStyleSnapshot
        {
            BackgroundColor = "#f8fafc",
            BackgroundOpacity = 1,
            BorderColor = "#cbd5e1",
            BorderOpacity = 1,
            BorderWidth = 1,
            TextColor = "#0f172a",
            TextOpacity = 0.9,
            FontSize = 12,
            FontBold = true
        })
    };
}
