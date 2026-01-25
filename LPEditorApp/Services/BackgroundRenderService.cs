using System.Globalization;
using System.Text.RegularExpressions;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class BackgroundRenderService
{
    public static string ResolveSourceType(BackgroundSetting setting)
    {
        var source = (setting.SourceType ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(source))
        {
            return source == "color" ? "solid" : source;
        }

        var fallback = (setting.Mode ?? string.Empty).Trim().ToLowerInvariant();
        return fallback == "color" ? "solid" : fallback;
    }

    public static bool UseMediaLayer(BackgroundSetting setting)
    {
        var type = ResolveSourceType(setting);
        return type is "image" or "video";
    }

    public static string BuildMediaStyle(BackgroundSetting setting)
    {
        var type = ResolveSourceType(setting);
        if (type == "image")
        {
            if (string.IsNullOrWhiteSpace(setting.ImageUrl))
            {
                return string.Empty;
            }

            var position = BackgroundStyleService.ResolvePosition(setting);
            var size = BackgroundStyleService.ResolveSize(setting);
            var repeat = string.IsNullOrWhiteSpace(setting.Repeat) ? "no-repeat" : setting.Repeat;
            return $"background-image:url(\"{EscapeCssUrl(setting.ImageUrl)}\");background-position:{position};background-size:{size};background-repeat:{repeat};";
        }

        return string.Empty;
    }

    public static string BuildFilterStyle(BackgroundEffects effects)
    {
        var hue = Clamp(effects.Hue ?? 0, -180, 180);
        var saturation = Clamp(effects.Saturation ?? 100, 0, 200);
        var brightness = Clamp(effects.Brightness ?? 100, 50, 150);
        var contrast = Clamp(effects.Contrast ?? 100, 50, 150);
        var blur = Clamp(effects.Blur ?? 0, 0, 10);

        return $"filter:hue-rotate({hue.ToString("0", CultureInfo.InvariantCulture)}deg) " +
               $"saturate({saturation.ToString("0", CultureInfo.InvariantCulture)}%) " +
               $"brightness({brightness.ToString("0", CultureInfo.InvariantCulture)}%) " +
               $"contrast({contrast.ToString("0", CultureInfo.InvariantCulture)}%) " +
               $"blur({blur.ToString("0.#", CultureInfo.InvariantCulture)}px);";
    }

    public static string BuildOverlayStyle(BackgroundEffects effects)
    {
        var overlay = effects.Overlay ?? new BackgroundOverlay();
        var opacity = Clamp(overlay.Opacity ?? 0, 0, 80) / 100d;
        if (opacity <= 0)
        {
            return "opacity:0;";
        }

        var color = SanitizeCssColor(overlay.Color) ?? "#000000";
        var blend = string.IsNullOrWhiteSpace(overlay.BlendMode) ? "normal" : overlay.BlendMode;
        return $"background:{color};opacity:{opacity.ToString("0.###", CultureInfo.InvariantCulture)};mix-blend-mode:{blend};";
    }

    public static string ResolveVideoUrl(BackgroundSetting setting)
    {
        if (!string.IsNullOrWhiteSpace(setting.VideoUrl))
        {
            return setting.VideoUrl;
        }

        return string.Empty;
    }

    public static string ResolveImageFallback(BackgroundSetting setting)
    {
        return string.IsNullOrWhiteSpace(setting.ImageUrl) ? string.Empty : setting.ImageUrl;
    }

    public static string ResolvePoster(BackgroundSetting setting)
    {
        return string.IsNullOrWhiteSpace(setting.VideoPoster) ? string.Empty : setting.VideoPoster;
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

    private static double Clamp(double value, double min, double max) => Math.Min(Math.Max(value, min), max);

    private static string EscapeCssUrl(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
