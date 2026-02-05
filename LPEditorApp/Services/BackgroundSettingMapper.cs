using LPEditorApp.Models;

namespace LPEditorApp.Services;

public static class BackgroundSettingMapper
{
    public static BackgroundSetting FromPage(LpBackgroundModel? model)
    {
        model ??= new LpBackgroundModel();
        var sourceType = NormalizeSourceType(model.SourceType, model.Mode);

        return new BackgroundSetting
        {
            SourceType = sourceType,
            Mode = string.IsNullOrWhiteSpace(model.Mode) ? sourceType : model.Mode,
            Color = string.IsNullOrWhiteSpace(model.Color) ? "transparent" : model.Color,
            ColorOpacity = model.ColorOpacity ?? 1,
            GradientType = string.IsNullOrWhiteSpace(model.GradientType) ? "linear" : model.GradientType,
            GradientAngle = model.GradientAngle ?? 135,
            GradientColorA = model.GradientColorA ?? string.Empty,
            GradientColorB = model.GradientColorB ?? string.Empty,
            GradientOpacity = model.GradientOpacity ?? 1,
            ImageUrl = model.ImageUrl ?? string.Empty,
            ImageUrlSp = model.ImageUrlSp ?? string.Empty,
            VideoUrl = model.VideoUrl ?? string.Empty,
            VideoUrlSp = model.VideoUrlSp ?? string.Empty,
            VideoPoster = model.VideoPoster ?? string.Empty,
            VideoPosterSp = model.VideoPosterSp ?? string.Empty,
            Repeat = string.IsNullOrWhiteSpace(model.Repeat) ? "repeat" : model.Repeat,
            Position = string.IsNullOrWhiteSpace(model.Position) ? "center center" : model.Position,
            PositionCustom = model.PositionCustom ?? string.Empty,
            Size = string.IsNullOrWhiteSpace(model.Size) ? "cover" : model.Size,
            SizeCustom = model.SizeCustom ?? string.Empty,
            Attachment = string.IsNullOrWhiteSpace(model.Attachment) ? "scroll" : model.Attachment,
            Preset = model.Preset ?? new BackgroundPresetSelection(),
            Effects = model.Effects ?? new BackgroundEffects()
        };
    }

    public static void ApplyToPage(BackgroundSetting setting, LpBackgroundModel model)
    {
        var sourceType = NormalizeSourceType(setting.SourceType, setting.Mode);
        model.SourceType = sourceType;
        model.Mode = string.IsNullOrWhiteSpace(setting.Mode) ? sourceType : setting.Mode;
        model.Color = setting.Color ?? string.Empty;
        model.ColorOpacity = setting.ColorOpacity;
        model.GradientType = string.IsNullOrWhiteSpace(setting.GradientType) ? "linear" : setting.GradientType;
        model.GradientAngle = setting.GradientAngle;
        model.GradientColorA = setting.GradientColorA ?? string.Empty;
        model.GradientColorB = setting.GradientColorB ?? string.Empty;
        model.GradientOpacity = setting.GradientOpacity;
        model.ImageUrl = setting.ImageUrl ?? string.Empty;
        model.ImageUrlSp = setting.ImageUrlSp ?? string.Empty;
        model.VideoUrl = setting.VideoUrl ?? string.Empty;
        model.VideoUrlSp = setting.VideoUrlSp ?? string.Empty;
        model.VideoPoster = setting.VideoPoster ?? string.Empty;
        model.VideoPosterSp = setting.VideoPosterSp ?? string.Empty;
        model.Repeat = string.IsNullOrWhiteSpace(setting.Repeat) ? "repeat" : setting.Repeat;
        model.Position = string.IsNullOrWhiteSpace(setting.Position) ? "center center" : setting.Position;
        model.PositionCustom = setting.PositionCustom ?? string.Empty;
        model.Size = string.IsNullOrWhiteSpace(setting.Size) ? "cover" : setting.Size;
        model.SizeCustom = setting.SizeCustom ?? string.Empty;
        model.Attachment = string.IsNullOrWhiteSpace(setting.Attachment) ? "scroll" : setting.Attachment;
        model.Preset = setting.Preset ?? new BackgroundPresetSelection();
        model.Effects = setting.Effects ?? new BackgroundEffects();
    }

    public static BackgroundSetting FromSection(SectionBackgroundSettings? settings)
    {
        settings ??= new SectionBackgroundSettings();
        var sourceType = NormalizeSourceType(settings.SourceType, settings.Mode);

        return new BackgroundSetting
        {
            SourceType = sourceType,
            Mode = string.IsNullOrWhiteSpace(settings.Mode) ? sourceType : settings.Mode,
            Color = settings.Color ?? string.Empty,
            ColorOpacity = settings.ColorOpacity ?? 1,
            GradientType = string.IsNullOrWhiteSpace(settings.GradientType) ? "linear" : settings.GradientType,
            GradientAngle = settings.GradientAngle ?? 135,
            GradientColorA = settings.GradientColorA ?? string.Empty,
            GradientColorB = settings.GradientColorB ?? string.Empty,
            GradientOpacity = settings.GradientOpacity ?? 1,
            ImageUrl = settings.ImageUrl ?? string.Empty,
            VideoUrl = settings.VideoUrl ?? string.Empty,
            VideoPoster = settings.VideoPoster ?? string.Empty,
            Repeat = string.IsNullOrWhiteSpace(settings.Repeat) ? "repeat" : settings.Repeat,
            Position = string.IsNullOrWhiteSpace(settings.Position) ? "center top" : settings.Position,
            PositionCustom = settings.PositionCustom ?? string.Empty,
            Size = string.IsNullOrWhiteSpace(settings.Size) ? "cover" : settings.Size,
            SizeCustom = settings.SizeCustom ?? string.Empty,
            Attachment = string.IsNullOrWhiteSpace(settings.Attachment) ? "scroll" : settings.Attachment,
            Preset = settings.Preset ?? new BackgroundPresetSelection(),
            Effects = settings.Effects ?? new BackgroundEffects()
        };
    }

    public static void ApplyToSection(BackgroundSetting setting, SectionBackgroundSettings settings)
    {
        var sourceType = NormalizeSourceType(setting.SourceType, setting.Mode);
        settings.SourceType = sourceType;
        settings.Mode = string.IsNullOrWhiteSpace(setting.Mode) ? sourceType : setting.Mode;
        settings.Color = setting.Color ?? string.Empty;
        settings.ColorOpacity = setting.ColorOpacity;
        settings.GradientType = string.IsNullOrWhiteSpace(setting.GradientType) ? "linear" : setting.GradientType;
        settings.GradientAngle = setting.GradientAngle;
        settings.GradientColorA = setting.GradientColorA ?? string.Empty;
        settings.GradientColorB = setting.GradientColorB ?? string.Empty;
        settings.GradientOpacity = setting.GradientOpacity;
        settings.ImageUrl = setting.ImageUrl ?? string.Empty;
        settings.VideoUrl = setting.VideoUrl ?? string.Empty;
        settings.VideoPoster = setting.VideoPoster ?? string.Empty;
        settings.Repeat = string.IsNullOrWhiteSpace(setting.Repeat) ? "repeat" : setting.Repeat;
        settings.Position = string.IsNullOrWhiteSpace(setting.Position) ? "center top" : setting.Position;
        settings.PositionCustom = setting.PositionCustom ?? string.Empty;
        settings.Size = string.IsNullOrWhiteSpace(setting.Size) ? "cover" : setting.Size;
        settings.SizeCustom = setting.SizeCustom ?? string.Empty;
        settings.Attachment = string.IsNullOrWhiteSpace(setting.Attachment) ? "scroll" : setting.Attachment;
        settings.Preset = setting.Preset ?? new BackgroundPresetSelection();
        settings.Effects = setting.Effects ?? new BackgroundEffects();
    }

    private static string NormalizeSourceType(string? sourceType, string? mode)
    {
        var normalized = (sourceType ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized == "color" ? "solid" : normalized;
        }

        var fallback = (mode ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fallback))
        {
            return "solid";
        }

        return fallback == "color" ? "solid" : fallback;
    }
}
