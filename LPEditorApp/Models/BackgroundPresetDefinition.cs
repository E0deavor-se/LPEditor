using System.Collections.ObjectModel;
using System.Linq;

namespace LPEditorApp.Models;

public enum BackgroundPresetKind
{
    Solid,
    Gradient,
    Pattern
}

public sealed class BackgroundPresetDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public BackgroundPresetKind Kind { get; init; }
    public string CssTemplate { get; init; } = string.Empty;

    public double DefaultAngle { get; init; } = 135;
    public string DefaultColorA { get; init; } = "#111827";
    public string DefaultColorB { get; init; } = "#ffffff";
    public double DefaultOpacity { get; init; } = 1;
    public double DefaultDensity { get; init; } = 1;
    public double BaseDensity { get; init; } = 18;
    public double MinDensityPx { get; init; } = 6;
    public double MaxDensityPx { get; init; } = 48;

    public bool SupportsAngle { get; init; }
    public bool SupportsColors { get; init; } = true;
    public bool SupportsOpacity { get; init; } = true;
    public bool SupportsDensity { get; init; }
}

public static class BackgroundPresetCatalog
{
    private static readonly List<BackgroundPresetDefinition> Presets = new()
    {
        new()
        {
            Key = "solid-ink",
            Label = "単色（ダーク）",
            Description = "落ち着いた単色背景",
            Kind = BackgroundPresetKind.Solid,
            CssTemplate = "{colorA}",
            DefaultColorA = "#0f172a",
            DefaultColorB = "#0f172a",
            SupportsAngle = false,
            SupportsDensity = false
        },
        new()
        {
            Key = "solid-paper",
            Label = "単色（ライト）",
            Description = "読みやすい明るい単色",
            Kind = BackgroundPresetKind.Solid,
            CssTemplate = "{colorA}",
            DefaultColorA = "#f8fafc",
            DefaultColorB = "#f8fafc",
            SupportsAngle = false,
            SupportsDensity = false
        },
        new()
        {
            Key = "gradient-royal",
            Label = "グラデーション（ロイヤル）",
            Description = "高級感のあるグラデ",
            Kind = BackgroundPresetKind.Gradient,
            CssTemplate = "linear-gradient({angle}deg, {colorA} 0%, {colorB} 100%)",
            DefaultColorA = "#312e81",
            DefaultColorB = "#f8fafc",
            SupportsAngle = true,
            SupportsDensity = false
        },
        new()
        {
            Key = "gradient-sunset",
            Label = "グラデーション（サンセット）",
            Description = "温かみのあるグラデ",
            Kind = BackgroundPresetKind.Gradient,
            CssTemplate = "linear-gradient({angle}deg, {colorA} 0%, {colorB} 100%)",
            DefaultColorA = "#be123c",
            DefaultColorB = "#fde68a",
            SupportsAngle = true,
            SupportsDensity = false
        },
        new()
        {
            Key = "pattern-dots",
            Label = "パターン（ドット）",
            Description = "柔らかいドット",
            Kind = BackgroundPresetKind.Pattern,
            CssTemplate = "radial-gradient(circle at 1px 1px, {colorA} {dotSize}px, transparent {dotSize}px) 0 0 / {densityPx}px {densityPx}px, {colorB}",
            DefaultColorA = "#0ea5e9",
            DefaultColorB = "#f8fafc",
            SupportsAngle = false,
            SupportsDensity = true
        },
        new()
        {
            Key = "pattern-stripes",
            Label = "パターン（ストライプ）",
            Description = "斜めストライプ",
            Kind = BackgroundPresetKind.Pattern,
            CssTemplate = "repeating-linear-gradient({angle}deg, {colorA} 0 2px, transparent 2px {densityPx}px), {colorB}",
            DefaultColorA = "#f97316",
            DefaultColorB = "#fff7ed",
            SupportsAngle = true,
            SupportsDensity = true
        },
        new()
        {
            Key = "pattern-grid",
            Label = "パターン（グリッド）",
            Description = "細かなグリッド",
            Kind = BackgroundPresetKind.Pattern,
            CssTemplate = "linear-gradient({colorA} 1px, transparent 1px) 0 0 / {densityPx}px {densityPx}px, linear-gradient(90deg, {colorA} 1px, transparent 1px) 0 0 / {densityPx}px {densityPx}px, {colorB}",
            DefaultColorA = "#94a3b8",
            DefaultColorB = "#f8fafc",
            SupportsAngle = false,
            SupportsDensity = true
        }
    };

    public static IReadOnlyList<BackgroundPresetDefinition> All => Presets.AsReadOnly();

    public static BackgroundPresetDefinition? Find(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Presets.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}
