using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class BackgroundSettingService
{
    public static void ApplyType(BackgroundSetting setting, string mode)
    {
        setting.SourceType = NormalizeSourceType(mode);
        setting.Mode = mode;
        ClearOtherValues(setting, mode);
    }

    public static void ApplyPreset(BackgroundSetting setting)
    {
        setting.Mode = "preset";
        setting.SourceType = "preset";
        ClearOtherValues(setting, "preset");
    }

    public static void ApplyImage(BackgroundSetting setting, string? imageUrl)
    {
        setting.Mode = "image";
        setting.SourceType = "image";
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            setting.ImageUrl = imageUrl;
        }
        ClearOtherValues(setting, "image");
    }

    public static void ApplyVideo(BackgroundSetting setting, string? videoUrl)
    {
        setting.Mode = "video";
        setting.SourceType = "video";
        if (!string.IsNullOrWhiteSpace(videoUrl))
        {
            setting.VideoUrl = videoUrl;
        }
        ClearOtherValues(setting, "video");
    }

    private static void ClearOtherValues(BackgroundSetting setting, string mode)
    {
        if (!string.Equals(mode, "image", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "video", StringComparison.OrdinalIgnoreCase))
        {
            setting.ImageUrl = string.Empty;
            setting.ImageUrlSp = string.Empty;
        }

        if (!string.Equals(mode, "video", StringComparison.OrdinalIgnoreCase))
        {
            setting.VideoUrl = string.Empty;
            setting.VideoPoster = string.Empty;
            setting.VideoUrlSp = string.Empty;
            setting.VideoPosterSp = string.Empty;
        }

        if (!string.Equals(mode, "preset", StringComparison.OrdinalIgnoreCase))
        {
            setting.Preset ??= new BackgroundPresetSelection();
            setting.Preset.PresetKey = string.Empty;
        }

        if (!string.Equals(mode, "solid", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "color", StringComparison.OrdinalIgnoreCase))
        {
            setting.Color = string.Empty;
            setting.ColorOpacity = null;
        }

        if (!string.Equals(mode, "gradient", StringComparison.OrdinalIgnoreCase))
        {
            setting.GradientType = "linear";
            setting.GradientAngle = null;
            setting.GradientColorA = string.Empty;
            setting.GradientColorB = string.Empty;
            setting.GradientOpacity = null;
        }

        if (!string.Equals(mode, "image", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mode, "video", StringComparison.OrdinalIgnoreCase))
        {
            setting.Effects = new BackgroundEffects();
        }
    }

    private static string NormalizeSourceType(string mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "color" ? "solid" : normalized;
    }
}
