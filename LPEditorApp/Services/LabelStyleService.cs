using System.Globalization;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class LabelStyleService
{
    public static string BuildCardStyle(StoreTargetLabelModel label)
    {
        if (label is null)
        {
            return string.Empty;
        }

        var fallback = BuildFallbackColors(label.Key);
        var bg = BuildColorWithOpacity(label.BackgroundColor, label.BackgroundOpacity, fallback.Background);
        var border = BuildColorWithOpacity(label.BorderColor, label.BorderOpacity, fallback.Border);
        return $"--label-card-bg:{bg};--label-card-border:{border};";
    }

    public static string BuildInlineStyle(StoreTargetLabelModel label)
    {
        if (label is null)
        {
            return string.Empty;
        }

        var fallback = BuildFallbackColors(label.Key);
        var bg = BuildColorWithOpacity(label.BackgroundColor, label.BackgroundOpacity, fallback.Background);
        var border = BuildColorWithOpacity(label.BorderColor, label.BorderOpacity, fallback.Border);
        var text = BuildColorWithOpacity(label.TextColor, label.TextOpacity, fallback.Text);
        var borderWidth = Math.Max(0, label.BorderWidth ?? 1);
        var fontSize = Math.Max(10, label.FontSize ?? 12);
        var fontWeight = label.FontBold == false ? 600 : 700;

        return $"background:{bg};border:{borderWidth}px solid {border};color:{text};font-size:{fontSize}px;font-weight:{fontWeight};";
    }

    public static string BuildInlineStyle(LabelStyleSnapshot style, string? fallbackKey = null)
    {
        var fallback = BuildFallbackColors(fallbackKey ?? string.Empty);
        var bg = BuildColorWithOpacity(style.BackgroundColor, style.BackgroundOpacity, fallback.Background);
        var border = BuildColorWithOpacity(style.BorderColor, style.BorderOpacity, fallback.Border);
        var text = BuildColorWithOpacity(style.TextColor, style.TextOpacity, fallback.Text);
        var borderWidth = Math.Max(0, style.BorderWidth ?? 1);
        var fontSize = Math.Max(10, style.FontSize ?? 12);
        var fontWeight = style.FontBold == false ? 600 : 700;

        return $"background:{bg};border:{borderWidth}px solid {border};color:{text};font-size:{fontSize}px;font-weight:{fontWeight};";
    }

    public static LabelStyleSnapshot ToSnapshot(StoreTargetLabelModel label)
    {
        return new LabelStyleSnapshot
        {
            BackgroundColor = label.BackgroundColor ?? string.Empty,
            BackgroundOpacity = label.BackgroundOpacity,
            BorderColor = label.BorderColor ?? string.Empty,
            BorderOpacity = label.BorderOpacity,
            BorderWidth = label.BorderWidth,
            TextColor = label.TextColor ?? string.Empty,
            TextOpacity = label.TextOpacity,
            FontSize = label.FontSize,
            FontBold = label.FontBold
        };
    }

    public static void ApplySnapshot(StoreTargetLabelModel label, LabelStyleSnapshot snapshot, LabelApplyTarget target)
    {
        if (target is LabelApplyTarget.Background or LabelApplyTarget.All)
        {
            label.BackgroundColor = snapshot.BackgroundColor ?? string.Empty;
            label.BackgroundOpacity = snapshot.BackgroundOpacity;
        }

        if (target is LabelApplyTarget.Border or LabelApplyTarget.All)
        {
            label.BorderColor = snapshot.BorderColor ?? string.Empty;
            label.BorderOpacity = snapshot.BorderOpacity;
            label.BorderWidth = snapshot.BorderWidth;
        }

        if (target is LabelApplyTarget.Text or LabelApplyTarget.All)
        {
            label.TextColor = snapshot.TextColor ?? string.Empty;
            label.TextOpacity = snapshot.TextOpacity;
            label.FontSize = snapshot.FontSize;
            label.FontBold = snapshot.FontBold;
        }

        label.UpdatedAt = DateTime.UtcNow;
    }

    public static string BuildColorWithOpacity(string? value, double? opacity, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = SanitizeCssColor(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        if (!TryParseHexColor(normalized, out var r, out var g, out var b, out var a))
        {
            return normalized;
        }

        var alpha = Math.Clamp(opacity ?? 1, 0, 1) * a;
        return $"rgba({r}, {g}, {b}, {alpha.ToString(CultureInfo.InvariantCulture)})";
    }

    public static bool TryParseHexColor(string value, out int r, out int g, out int b, out double a)
    {
        r = g = b = 0;
        a = 1;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim();
        if (hex.StartsWith("#", StringComparison.Ordinal))
        {
            hex = hex[1..];
        }

        if (hex.Length is not (3 or 4 or 6 or 8))
        {
            return false;
        }

        if (hex.Length is 3 or 4)
        {
            var chars = hex.ToCharArray();
            hex = string.Concat(chars.Select(c => $"{c}{c}"));
        }

        var hasAlpha = hex.Length == 8;
        if (!int.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r))
        {
            return false;
        }
        if (!int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
        {
            return false;
        }
        if (!int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
        {
            return false;
        }

        if (hasAlpha && int.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var alpha))
        {
            a = Math.Clamp(alpha / 255d, 0, 1);
        }

        return true;
    }

    public static string? SanitizeCssColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = value.Trim();
        if (sanitized.Contains(';', StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace(";", string.Empty, StringComparison.Ordinal);
        }

        return sanitized;
    }

    public static string GetColorFamily(StoreTargetLabelModel label)
    {
        if (!TryParseHexColor(label.BackgroundColor, out var r, out var g, out var b, out _))
        {
            return "other";
        }

        var (h, s, _) = RgbToHsl(r, g, b);
        if (s < 0.12)
        {
            return "mono";
        }
        if (h < 35 || h > 325)
        {
            return "warm";
        }
        if (h >= 35 && h < 180)
        {
            return "green";
        }
        if (h >= 180 && h < 260)
        {
            return "cool";
        }

        return "purple";
    }

    public static double GetContrastRatio(string? textColor, double? textOpacity, string? backgroundColor, double? backgroundOpacity)
    {
        if (!TryParseHexColor(textColor ?? string.Empty, out var tr, out var tg, out var tb, out var ta))
        {
            return 0;
        }

        if (!TryParseHexColor(backgroundColor ?? string.Empty, out var br, out var bg, out var bb, out var ba))
        {
            return 0;
        }

        var textAlpha = Math.Clamp(textOpacity ?? 1, 0, 1) * ta;
        var bgAlpha = Math.Clamp(backgroundOpacity ?? 1, 0, 1) * ba;

        var textLum = RelativeLuminance(BlendOnWhite(tr, tg, tb, textAlpha));
        var bgLum = RelativeLuminance(BlendOnWhite(br, bg, bb, bgAlpha));

        var lighter = Math.Max(textLum, bgLum);
        var darker = Math.Min(textLum, bgLum);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static (string Background, string Border, string Text) BuildFallbackColors(string key)
    {
        var hash = Math.Abs((key ?? string.Empty).GetHashCode());
        var hue = hash % 360;
        return ($"hsl({hue}, 90%, 92%)", $"hsl({hue}, 80%, 75%)", $"hsl({hue}, 65%, 30%)");
    }

    private static (double H, double S, double L) RgbToHsl(int r, int g, int b)
    {
        var rf = r / 255d;
        var gf = g / 255d;
        var bf = b / 255d;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        var h = 0d;
        if (delta > 0)
        {
            if (max == rf)
            {
                h = (gf - bf) / delta % 6;
            }
            else if (max == gf)
            {
                h = (bf - rf) / delta + 2;
            }
            else
            {
                h = (rf - gf) / delta + 4;
            }
            h *= 60;
            if (h < 0)
            {
                h += 360;
            }
        }

        var l = (max + min) / 2;
        var s = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * l - 1));
        return (h, s, l);
    }

    private static (int R, int G, int B) BlendOnWhite(int r, int g, int b, double alpha)
    {
        var a = Math.Clamp(alpha, 0, 1);
        var br = (int)Math.Round((1 - a) * 255 + a * r);
        var bg = (int)Math.Round((1 - a) * 255 + a * g);
        var bb = (int)Math.Round((1 - a) * 255 + a * b);
        return (br, bg, bb);
    }

    private static double RelativeLuminance((int R, int G, int B) rgb)
    {
        double Channel(int c)
        {
            var v = c / 255d;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(rgb.R) + 0.7152 * Channel(rgb.G) + 0.0722 * Channel(rgb.B);
    }
}

public enum LabelApplyTarget
{
    Background,
    Border,
    Text,
    All
}
