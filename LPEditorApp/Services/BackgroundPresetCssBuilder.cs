using System.Globalization;
using System.Text.RegularExpressions;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class BackgroundPresetCssBuilder
{
    public static string? BuildBackgroundValue(BackgroundPresetSelection? selection)
    {
        // プリセット + オーバーライドから最終CSSを生成（Editor/Preview共通）。
        if (selection is null || string.IsNullOrWhiteSpace(selection.PresetKey))
        {
            return null;
        }

        var preset = BackgroundPresetCatalog.Find(selection.PresetKey);
        return preset is null ? null : BuildBackgroundValue(preset, selection);
    }

    public static string BuildBackgroundValue(BackgroundPresetDefinition preset, BackgroundPresetSelection selection)
    {
        var angle = Clamp(selection.Angle ?? preset.DefaultAngle, 0, 360);
        var opacity = Clamp(selection.Opacity ?? preset.DefaultOpacity, 0, 1);
        var densityScale = Clamp(selection.Density ?? preset.DefaultDensity, 0.5, 2);

        var colorA = ResolveColor(selection.ColorA, preset.DefaultColorA, opacity);
        var colorB = ResolveColor(selection.ColorB, preset.DefaultColorB, opacity);

        var densityPx = Clamp(preset.BaseDensity * densityScale, preset.MinDensityPx, preset.MaxDensityPx);
        var dotSize = Math.Max(1, Math.Round(densityPx * 0.12, 2));

        return preset.CssTemplate
            .Replace("{angle}", angle.ToString(CultureInfo.InvariantCulture))
            .Replace("{colorA}", colorA)
            .Replace("{colorB}", colorB)
            .Replace("{densityPx}", densityPx.ToString(CultureInfo.InvariantCulture))
            .Replace("{dotSize}", dotSize.ToString(CultureInfo.InvariantCulture));
    }

    public static string BuildPreviewValue(BackgroundPresetDefinition preset)
    {
        // Hoverプレビューはプリセット既定値で表示する。
        var selection = new BackgroundPresetSelection
        {
            PresetKey = preset.Key,
            Angle = preset.DefaultAngle,
            ColorA = preset.DefaultColorA,
            ColorB = preset.DefaultColorB,
            Opacity = preset.DefaultOpacity,
            Density = preset.DefaultDensity
        };

        return BuildBackgroundValue(preset, selection);
    }

    private static string ResolveColor(string? value, string fallback, double opacity)
    {
        var normalized = NormalizeHexColor(value) ?? NormalizeHexColor(fallback) ?? "#000000";
        if (!TryParseHex(normalized, out var r, out var g, out var b, out var a))
        {
            return normalized;
        }

        var alpha = Math.Clamp(a * opacity, 0, 1);
        return $"rgba({r}, {g}, {b}, {alpha.ToString(CultureInfo.InvariantCulture)})";
    }

    private static string? NormalizeHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, "^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$") ? trimmed : null;
    }

    private static bool TryParseHex(string hex, out int r, out int g, out int b, out double a)
    {
        r = g = b = 0;
        a = 1;

        if (hex.Length == 7)
        {
            r = Convert.ToInt32(hex.Substring(1, 2), 16);
            g = Convert.ToInt32(hex.Substring(3, 2), 16);
            b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return true;
        }

        if (hex.Length == 9)
        {
            r = Convert.ToInt32(hex.Substring(1, 2), 16);
            g = Convert.ToInt32(hex.Substring(3, 2), 16);
            b = Convert.ToInt32(hex.Substring(5, 2), 16);
            var alpha = Convert.ToInt32(hex.Substring(7, 2), 16);
            a = Math.Round(alpha / 255d, 3);
            return true;
        }

        return false;
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
