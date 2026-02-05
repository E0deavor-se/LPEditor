using System.Globalization;
using System.Text.RegularExpressions;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class BackgroundStyleService
{
    public static string BuildInlineStyle(BackgroundSetting setting, bool includeMediaImage = true)
    {
        var rules = BuildInlineRules(setting, includeImportant: false, includeMediaImage: includeMediaImage);
        return rules.Count == 0 ? string.Empty : string.Join(" ", rules);
    }

    public static string BuildRule(BackgroundSetting setting, string selector, bool includeMediaImage = true)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return string.Empty;
        }

        var rules = BuildInlineRules(setting, includeImportant: true, includeMediaImage: includeMediaImage);
        return rules.Count == 0 ? string.Empty : $"{selector} {{ {string.Join(" ", rules)} }}";
    }

    public static List<string> BuildInlineRules(BackgroundSetting setting, bool includeImportant, bool includeMediaImage = true)
    {
        var rules = new List<string>();
        var mode = BackgroundRenderService.ResolveSourceType(setting);
        var important = includeImportant ? " !important" : string.Empty;

        if (mode == "inherit")
        {
            return rules;
        }

        if (mode == "preset")
        {
            var presetValue = BackgroundPresetCssBuilder.BuildBackgroundValue(setting.Preset);
            if (!string.IsNullOrWhiteSpace(presetValue))
            {
                rules.Add($"background: {presetValue}{important};");
            }

            return rules;
        }

        if (mode is "solid" or "color")
        {
            var color = ResolveColor(setting.Color, setting.ColorOpacity);
            if (!string.IsNullOrWhiteSpace(color))
            {
                rules.Add($"background-color: {color}{important};");
                rules.Add($"background-image: none{important};");
            }

            return rules;
        }

        if (mode == "gradient")
        {
            var colorA = ResolveColor(setting.GradientColorA, setting.GradientOpacity);
            var colorB = ResolveColor(setting.GradientColorB, setting.GradientOpacity);
            if (string.IsNullOrWhiteSpace(colorA) || string.IsNullOrWhiteSpace(colorB))
            {
                return rules;
            }

            var gradient = BuildGradientValue(setting, colorA, colorB);
            rules.Add($"background-image: {gradient}{important};");
            rules.Add($"background-color: {colorA}{important};");
            return rules;
        }

        if (mode == "image")
        {
            if (!includeMediaImage)
            {
                return rules;
            }
            var imageUrl = ResolveImageUrl(setting);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return rules;
            }

            var repeat = string.IsNullOrWhiteSpace(setting.Repeat) ? "no-repeat" : setting.Repeat;
            var position = ResolvePosition(setting);
            var size = ResolveSize(setting);
            var attachment = string.IsNullOrWhiteSpace(setting.Attachment) ? "scroll" : setting.Attachment;
            var image = $"url(\"{EscapeCssUrl(imageUrl)}\")";
            rules.Add($"background-image: {image}{important};");
            rules.Add($"background-repeat: {repeat}{important};");
            rules.Add($"background-position: {position}{important};");
            rules.Add($"background-size: {size}{important};");
            rules.Add($"background-attachment: {attachment}{important};");
        }

        if (mode == "video")
        {
            return rules;
        }

        return rules;
    }

    private static string? ResolveImageUrl(BackgroundSetting setting)
    {
        if (!string.IsNullOrWhiteSpace(setting.ImageUrl))
        {
            return setting.ImageUrl;
        }

        return string.IsNullOrWhiteSpace(setting.ImageUrlSp) ? null : setting.ImageUrlSp;
    }

    public static string ResolvePosition(BackgroundSetting setting)
    {
        if (string.Equals(setting.Position, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(setting.PositionCustom) ? "center center" : setting.PositionCustom;
        }

        return string.IsNullOrWhiteSpace(setting.Position) ? "center center" : setting.Position;
    }

    public static string ResolveSize(BackgroundSetting setting)
    {
        if (string.Equals(setting.Size, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(setting.SizeCustom) ? "cover" : setting.SizeCustom;
        }

        return string.IsNullOrWhiteSpace(setting.Size) ? "cover" : setting.Size;
    }

    private static string BuildGradientValue(BackgroundSetting setting, string colorA, string colorB)
    {
        var type = setting.GradientType?.Trim().ToLowerInvariant() ?? "linear";
        var angle = Math.Clamp(setting.GradientAngle ?? 135, 0, 360);

        return type == "radial"
            ? $"radial-gradient(circle at center, {colorA}, {colorB})"
            : $"linear-gradient({angle.ToString("0", CultureInfo.InvariantCulture)}deg, {colorA}, {colorB})";
    }

    private static string? ResolveColor(string? value, double? opacity)
    {
        var sanitized = SanitizeCssColor(value);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        var alpha = Math.Clamp(opacity ?? 1, 0, 1);
        if (alpha >= 0.999)
        {
            return sanitized;
        }

        if (TryParseHexColor(sanitized, out var r, out var g, out var b, out var a))
        {
            var finalAlpha = Math.Clamp(a * alpha, 0, 1);
            return $"rgba({r}, {g}, {b}, {finalAlpha.ToString("0.###", CultureInfo.InvariantCulture)})";
        }

        return sanitized;
    }

    private static string? SanitizeCssColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, "^[#a-zA-Z0-9(),.%\\s]+$") ? trimmed : null;
    }

    private static bool TryParseHexColor(string value, out int r, out int g, out int b, out double a)
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
            var expanded = new char[hex.Length * 2];
            for (var i = 0; i < hex.Length; i++)
            {
                expanded[i * 2] = hex[i];
                expanded[i * 2 + 1] = hex[i];
            }

            hex = new string(expanded);
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

    private static string EscapeCssUrl(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
