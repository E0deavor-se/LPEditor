using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using LPEditorApp.Models;
using LPEditorApp.Services;

namespace LPEditorApp.Components.Shared;

public partial class CardThemeEditor : ComponentBase
{
    private static readonly Regex HexRegex = new("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    [Parameter] public FrameStyle Style { get; set; } = new();
    [Parameter] public IReadOnlyList<FontFamilyOption> FontOptions { get; set; } = Array.Empty<FontFamilyOption>();
    [Parameter] public IReadOnlyList<string> RecentColors { get; set; } = Array.Empty<string>();
    [Parameter] public string ScopeLabel { get; set; } = "フレーム（共通）";
    [Parameter] public string ScopeNote { get; set; } = "全セクションに適用";
    [Parameter] public EventCallback<string> OnColorUsed { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback OnBandChanged { get; set; }
    [Parameter] public EventCallback OnReset { get; set; }
    [Parameter] public bool EnableAnimationTab { get; set; }
    [Parameter] public FrameStyle? AnimationStyle { get; set; }
    [Parameter] public bool ShowAnimationScopeToggle { get; set; }
    [Parameter] public string AnimationScope { get; set; } = "per";
    [Parameter] public EventCallback<string> OnAnimationScopeChanged { get; set; }
    [Parameter] public bool AnimationPreviewEnabled { get; set; } = true;
    [Parameter] public EventCallback<bool> OnAnimationPreviewToggled { get; set; }

    [Inject] public FramePresetService FramePresetService { get; set; } = default!;
    [Inject] public AnimationPresetService AnimationPresetService { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    private static readonly IReadOnlyList<string> RecommendedColors = new[]
    {
        "#ffffff", "#f8fafc", "#fee2e2", "#fef2f2", "#fde68a", "#fef3c7", "#e0f2fe", "#dbeafe", "#ede9fe", "#ecfccb"
    };

    private static readonly IReadOnlyList<KeyValuePair<string, string>> ShadowOptions = new[]
    {
        new KeyValuePair<string, string>("off", "Off"),
        new KeyValuePair<string, string>("sm", "Sm"),
        new KeyValuePair<string, string>("md", "Md"),
        new KeyValuePair<string, string>("lg", "Lg")
    };

    private static readonly Dictionary<string, string> PresetCategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["simple"] = "シンプル",
        ["line"] = "ライン",
        ["emphasis"] = "強調",
        ["note"] = "ノート",
        ["ribbon"] = "リボン"
    };

    private IReadOnlyList<FramePreset> Presets = Array.Empty<FramePreset>();
    private string PresetSearch = string.Empty;
    private string SelectedPresetCategory = "all";
    private bool PresetPanelOpen = true;
    private string? PresetLoadError;
    private FrameStyle? UndoSnapshot;
    private bool IsApplyingPreset;
    private HashSet<string> FavoritePresets = new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<AnimationPreset> AnimPresets = Array.Empty<AnimationPreset>();
    private string AnimSearch = string.Empty;
    private string SelectedAnimCategory = "all";
    private string? AnimLoadError;
    private string ActiveEditorTab = "design";

    private const string FrameAnimationKey = "frame";
    private static readonly string[] LegacyAnimationKeys =
    {
        FrameAnimationKey, "outer", "band", "inner", "content", "corner-tl", "corner-tr", "corner-bl", "corner-br", "tab"
    };

    private static readonly Dictionary<string, string> AnimCategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["simple"] = "シンプル",
        ["pop"] = "ポップ",
        ["stylish"] = "スタイリッシュ",
        ["soft"] = "やわらかい"
    };

    private static readonly IReadOnlyList<KeyValuePair<string, string>> AnimEasingOptions = new[]
    {
        new KeyValuePair<string, string>("ease", "ease"),
        new KeyValuePair<string, string>("ease-in", "ease-in"),
        new KeyValuePair<string, string>("ease-out", "ease-out"),
        new KeyValuePair<string, string>("ease-in-out", "ease-in-out"),
        new KeyValuePair<string, string>("cubic-bezier(0.16, 1, 0.3, 1)", "smooth")
    };

    private static string GetChipClass(bool isActive) => isActive ? "lp-chip is-active" : "lp-chip";
    private static string GetOptionClass(bool isActive) => isActive ? "lp-option-card is-selected" : "lp-option-card";
    private static string GetSwatchStyle(string color) => $"background:{color}";

    private bool IsType(string type) => string.Equals(Style.Type, type, StringComparison.OrdinalIgnoreCase);
    private bool IsShadow(string value) => string.Equals(Style.ShadowLevel, value, StringComparison.OrdinalIgnoreCase);

    private bool CanUndo => UndoSnapshot is not null;
    private string CurrentPresetLabel => ResolveCurrentPresetLabel();

    private IReadOnlyList<string> PresetCategories => Presets
        .Select(p => p.Category)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(c => c)
        .ToList();

    private IReadOnlyList<FramePreset> FilteredPresets => Presets
        .Where(p => MatchesCategory(p))
        .Where(p => MatchesSearch(p))
        .ToList();

    private IReadOnlyList<string> AnimCategories => AnimPresets
        .Select(p => p.Category)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(c => c)
        .ToList();

    private IReadOnlyList<AnimationPreset> FilteredAnimPresets => AnimPresets
        .Where(p => MatchesAnimCategory(p))
        .Where(p => MatchesAnimSearch(p))
        .ToList();

    private FrameStyle ActiveAnimationStyle => AnimationStyle ?? Style;

    private FrameAnimationTargetSetting ActiveAnimSetting => EnsureAnimSetting();

    private int AnimDuration
    {
        get => Math.Clamp(ActiveAnimSetting.DurationMs ?? 600, 200, 2400);
        set => ActiveAnimSetting.DurationMs = Math.Clamp(value, 200, 2400);
    }

    private int AnimDelay
    {
        get => Math.Clamp(ActiveAnimSetting.DelayMs ?? 0, 0, 2000);
        set => ActiveAnimSetting.DelayMs = Math.Clamp(value, 0, 2000);
    }

    private string AnimEasing
    {
        get => string.IsNullOrWhiteSpace(ActiveAnimSetting.Easing) ? "ease" : ActiveAnimSetting.Easing;
        set => ActiveAnimSetting.Easing = value;
    }

    private string AnimTrigger => ActiveAnimSetting.Trigger;

    private bool AnimLoop
    {
        get => ActiveAnimSetting.Loop;
        set => ActiveAnimSetting.Loop = value;
    }

    private bool IsEditorTab(string tab) => string.Equals(ActiveEditorTab, tab, StringComparison.OrdinalIgnoreCase);

    private void SetEditorTab(string tab)
    {
        ActiveEditorTab = tab;
    }

    private bool BackgroundColorInvalid => IsInvalidColor(Style.BackgroundColor);
    private bool BorderColorInvalid => IsInvalidColor(Style.BorderColor);
    private bool HeaderBackgroundInvalid => IsInvalidColor(Style.HeaderBackgroundColor);
    private bool HeaderTextInvalid => IsInvalidColor(Style.HeaderTextColor);

    private int BackgroundOpacityPercent
    {
        get => Math.Clamp(Style.BackgroundOpacity ?? 100, 0, 100);
        set => Style.BackgroundOpacity = Math.Clamp(value, 0, 100);
    }

    private int BorderWidth
    {
        get => Math.Clamp(Style.BorderWidth ?? 0, 0, 12);
        set => Style.BorderWidth = Math.Clamp(value, 0, 12);
    }

    private int BorderRadius
    {
        get => Math.Clamp(Style.BorderRadius ?? 0, 0, 32);
        set => Style.BorderRadius = Math.Clamp(value, 0, 32);
    }

    private int PaddingX
    {
        get => Math.Clamp(Style.PaddingX ?? 0, 0, 48);
        set => Style.PaddingX = Math.Clamp(value, 0, 48);
    }

    private int PaddingY
    {
        get => Math.Clamp(Style.PaddingY ?? 0, 0, 48);
        set => Style.PaddingY = Math.Clamp(value, 0, 48);
    }

    private int MaxWidthPx
    {
        get => Math.Clamp(Style.MaxWidthPx ?? 760, 520, 900);
        set => Style.MaxWidthPx = Math.Clamp(value, 520, 900);
    }

    private int HeaderHeightPx
    {
        get => Math.Clamp(Style.HeaderHeightPx ?? 42, 28, 80);
        set => Style.HeaderHeightPx = Math.Clamp(value, 28, 80);
    }

    private int HeaderFontSizePx
    {
        get => Math.Clamp(Style.HeaderFontSizePx ?? 18, 12, 36);
        set => Style.HeaderFontSizePx = Math.Clamp(value, 12, 36);
    }

    private int BodyFontSizePx
    {
        get => Math.Clamp(Style.BodyFontSizePx ?? 16, 12, 28);
        set => Style.BodyFontSizePx = Math.Clamp(value, 12, 28);
    }

    protected override async Task OnInitializedAsync()
    {
        Presets = await FramePresetService.GetPresetsAsync();
        PresetLoadError = FramePresetService.LastErrorMessage;
        AnimPresets = await AnimationPresetService.GetPresetsAsync();
        AnimLoadError = AnimationPresetService.LastErrorMessage;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var favorites = await JS.InvokeAsync<string[]>("lpFramePresets.loadFavorites");
            FavoritePresets = new HashSet<string>(favorites ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
        }
    }

    private async Task NotifyChanged()
    {
        MarkCustomIfNeeded();
        await OnChanged.InvokeAsync();
    }

    private async Task NotifyBandChanged()
    {
        MarkCustomIfNeeded();
        if (OnBandChanged.HasDelegate)
        {
            await OnBandChanged.InvokeAsync();
        }
        await OnChanged.InvokeAsync();
    }

    private async Task OnColorChangedAfter(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            await OnColorUsed.InvokeAsync(value);
        }
        MarkCustomIfNeeded();
        await OnChanged.InvokeAsync();
    }

    private async Task OnBandColorChangedAfter(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            await OnColorUsed.InvokeAsync(value);
        }
        MarkCustomIfNeeded();
        if (OnBandChanged.HasDelegate)
        {
            await OnBandChanged.InvokeAsync();
        }
        await OnChanged.InvokeAsync();
    }

    private async Task ApplyPaletteColor(string color)
    {
        Style.BackgroundColor = color;
        MarkCustomIfNeeded();
        await OnColorUsed.InvokeAsync(color);
        await OnChanged.InvokeAsync();
    }

    private async Task OnPaddingPresetChanged()
    {
        if (!string.Equals(Style.PaddingPreset, "custom", StringComparison.OrdinalIgnoreCase))
        {
            var preset = Style.PaddingPreset ?? "normal";
            (Style.PaddingX, Style.PaddingY) = preset switch
            {
                "compact" => (16, 12),
                "spacious" => (32, 24),
                _ => (24, 20)
            };
        }
        MarkCustomIfNeeded();
        await OnChanged.InvokeAsync();
    }

    private async Task SetShadow(string value)
    {
        Style.ShadowLevel = value;
        MarkCustomIfNeeded();
        await OnChanged.InvokeAsync();
    }

    private async Task SetType(string type)
    {
        Style.Type = type;
        switch (type)
        {
            case "simple":
                Style.BackgroundColor = "#ffffff";
                Style.BackgroundOpacity = 100;
                Style.BorderColor = "#e11d48";
                Style.BorderWidth = 1;
                Style.ShadowLevel = "off";
                Style.HeaderBackgroundColor = "#e11d48";
                Style.HeaderTextColor = "#ffffff";
                break;
            case "color":
                Style.BackgroundColor ??= "#fee2e2";
                Style.BackgroundOpacity ??= 100;
                Style.BorderColor ??= "#dc2626";
                Style.BorderWidth ??= 2;
                Style.ShadowLevel = "md";
                Style.HeaderBackgroundColor ??= "#dc2626";
                Style.HeaderTextColor ??= "#ffffff";
                break;
            case "emphasis":
                Style.BackgroundColor ??= "#fff5f5";
                Style.BorderColor = "#b91c1c";
                Style.BorderWidth = 3;
                Style.ShadowLevel = "lg";
                Style.HeaderBackgroundColor = "#b91c1c";
                Style.HeaderTextColor = "#ffffff";
                Style.HeaderHeightPx ??= 48;
                break;
        }
        MarkCustomIfNeeded();
        await OnChanged.InvokeAsync();
    }

    private async Task OnAnimationPreviewToggle(ChangeEventArgs args)
    {
        if (args.Value is bool enabled)
        {
            await OnAnimationPreviewToggled.InvokeAsync(enabled);
        }
        else
        {
            var next = args.Value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            await OnAnimationPreviewToggled.InvokeAsync(next);
        }
    }

    private async Task SetAnimationScope(string scope)
    {
        await OnAnimationScopeChanged.InvokeAsync(scope);
    }

    private bool IsAnimCategory(string category) => string.Equals(SelectedAnimCategory, category, StringComparison.OrdinalIgnoreCase);

    private void SetAnimCategory(string category)
    {
        SelectedAnimCategory = category;
    }

    private string GetAnimCategoryLabel(string category)
    {
        if (AnimCategoryLabels.TryGetValue(category, out var label))
        {
            return label;
        }

        return category;
    }

    private bool MatchesAnimCategory(AnimationPreset preset)
    {
        if (string.Equals(SelectedAnimCategory, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(preset.Category, SelectedAnimCategory, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesAnimSearch(AnimationPreset preset)
    {
        if (string.IsNullOrWhiteSpace(AnimSearch))
        {
            return true;
        }

        var needle = AnimSearch.Trim();
        return preset.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || preset.RecommendedUse.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAnimPresetSelected(string id)
    {
        return string.Equals(ActiveAnimSetting.PresetId, id, StringComparison.OrdinalIgnoreCase);
    }

    private string GetAnimPreviewStyle(AnimationPreset preset)
    {
        var duration = preset.DefaultDurationMs;
        var delay = preset.DefaultDelayMs;
        var easing = string.IsNullOrWhiteSpace(preset.DefaultEasing) ? "ease" : preset.DefaultEasing;
        return $"--anim-duration: {duration}ms; --anim-delay: {delay}ms; --anim-ease: {easing};";
    }

    private async Task ApplyAnimPresetAsync(AnimationPreset preset)
    {
        var setting = EnsureAnimSetting();
        setting.PresetId = preset.Id;
        setting.DurationMs = preset.DefaultDurationMs;
        setting.DelayMs = preset.DefaultDelayMs;
        setting.Easing = preset.DefaultEasing;
        setting.SpDurationRate = preset.SpDurationRate;
        setting.Enabled = true;
        await OnChanged.InvokeAsync();
    }

    private async Task OnAnimDetailChanged()
    {
        await OnChanged.InvokeAsync();
    }

    private async Task SetAnimTrigger(string trigger)
    {
        ActiveAnimSetting.Trigger = trigger;
        await OnChanged.InvokeAsync();
    }

    private async Task ResetAnimationTarget()
    {
        var setting = EnsureAnimSetting();
        setting.PresetId = "none";
        setting.Enabled = false;
        await OnChanged.InvokeAsync();
    }

    private FrameAnimationTargetSetting EnsureAnimSetting()
    {
        var targets = ActiveAnimationStyle.AnimationTargets;
        if (targets.TryGetValue(FrameAnimationKey, out var setting) && setting is not null)
        {
            if (targets.Count > 1)
            {
                targets.Clear();
                targets[FrameAnimationKey] = setting;
            }

            return setting;
        }

        foreach (var key in LegacyAnimationKeys)
        {
            if (targets.TryGetValue(key, out var legacy) && legacy is not null)
            {
                targets.Clear();
                targets[FrameAnimationKey] = legacy;
                return legacy;
            }
        }

        setting = new FrameAnimationTargetSetting();
        targets.Clear();
        targets[FrameAnimationKey] = setting;
        return setting;
    }

    private void TogglePresetPanel()
    {
        PresetPanelOpen = !PresetPanelOpen;
    }

    private string ResolveCurrentPresetLabel()
    {
        if (!string.IsNullOrWhiteSpace(Style.PresetKey))
        {
            var match = Presets.FirstOrDefault(p => string.Equals(p.Id, Style.PresetKey, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.Name;
            }
        }

        return "Custom";
    }

    private async Task ClearPresetSelection()
    {
        Style.PresetKey = null;
        await OnChanged.InvokeAsync();
    }

    private bool IsPresetSelected(string id) => string.Equals(Style.PresetKey, id, StringComparison.OrdinalIgnoreCase);

    private bool IsPresetCategory(string category) => string.Equals(SelectedPresetCategory, category, StringComparison.OrdinalIgnoreCase);

    private void SetPresetCategory(string category)
    {
        SelectedPresetCategory = category;
    }

    private string GetPresetCategoryLabel(string category)
    {
        if (PresetCategoryLabels.TryGetValue(category, out var label))
        {
            return label;
        }

        return category;
    }

    private bool MatchesCategory(FramePreset preset)
    {
        if (string.Equals(SelectedPresetCategory, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(SelectedPresetCategory, "favorites", StringComparison.OrdinalIgnoreCase))
        {
            return FavoritePresets.Contains(preset.Id);
        }

        return string.Equals(preset.Category, SelectedPresetCategory, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSearch(FramePreset preset)
    {
        if (string.IsNullOrWhiteSpace(PresetSearch))
        {
            return true;
        }

        var needle = PresetSearch.Trim();
        return preset.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || preset.Description.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private string GetPresetPreviewStyle(FramePreset preset)
    {
        var preview = preset.Preview;
        var background = preview.BackgroundColor ?? preset.Apply.BackgroundColor ?? "#ffffff";
        var opacity = preview.BackgroundOpacity ?? preset.Apply.BackgroundOpacity ?? 100;
        var bg = BuildRgba(background, opacity);
        var borderColor = preview.BorderColor ?? preset.Apply.BorderColor ?? "#e2e8f0";
        var borderWidth = preview.BorderWidth ?? preset.Apply.BorderWidth ?? 1;
        var borderStyle = preview.BorderStyle ?? preset.Apply.BorderStyle ?? "solid";
        var radius = preview.BorderRadius ?? preset.Apply.BorderRadius ?? 12;

        var shadow = ResolveShadow(preview.ShadowLevel ?? preset.Apply.ShadowLevel);

        var gradientA = preview.BorderGradientColorA ?? preset.Apply.BorderGradientColorA;
        var gradientB = preview.BorderGradientColorB ?? preset.Apply.BorderGradientColorB;
        var gradientAngle = preview.BorderGradientAngle ?? preset.Apply.BorderGradientAngle ?? 135;
        var borderRule = !string.IsNullOrWhiteSpace(gradientA) && !string.IsNullOrWhiteSpace(gradientB)
            ? $"border: {borderWidth}px solid transparent; border-image: linear-gradient({gradientAngle}deg, {gradientA}, {gradientB}) 1"
            : $"border: {borderWidth}px {borderStyle} {borderColor}";

        return $"background: {bg}; {borderRule}; border-radius: {radius}px; box-shadow: {shadow};";
    }

    private async Task ApplyPresetAsync(FramePreset preset)
    {
        CaptureUndoSnapshot();

        IsApplyingPreset = true;
        try
        {
            ApplyPresetValues(preset);
            Style.PresetKey = preset.Id;
            await OnChanged.InvokeAsync();
        }
        finally
        {
            IsApplyingPreset = false;
        }
    }

    private void ApplyPresetValues(FramePreset preset)
    {
        var apply = preset.Apply;

        Style.BackgroundColor = apply.BackgroundColor ?? Style.BackgroundColor;
        Style.BackgroundOpacity = apply.BackgroundOpacity ?? Style.BackgroundOpacity;
        Style.BorderColor = apply.BorderColor ?? Style.BorderColor;
        Style.BorderWidth = apply.BorderWidth ?? Style.BorderWidth;
        Style.BorderStyle = apply.BorderStyle ?? Style.BorderStyle;
        Style.BorderRadius = apply.BorderRadius ?? Style.BorderRadius;
        Style.ShadowLevel = apply.ShadowLevel ?? Style.ShadowLevel;
        Style.PaddingPreset = apply.PaddingPreset ?? "custom";
        Style.PaddingX = apply.PaddingX ?? Style.PaddingX;
        Style.PaddingY = apply.PaddingY ?? Style.PaddingY;
        Style.BorderGradientColorA = apply.BorderGradientColorA ?? Style.BorderGradientColorA;
        Style.BorderGradientColorB = apply.BorderGradientColorB ?? Style.BorderGradientColorB;
        Style.BorderGradientAngle = apply.BorderGradientAngle ?? Style.BorderGradientAngle;
    }

    private async Task UndoPresetAsync()
    {
        if (UndoSnapshot is null)
        {
            return;
        }

        CopyStyle(UndoSnapshot, Style);
        UndoSnapshot = null;
        await OnChanged.InvokeAsync();
    }

    private void CaptureUndoSnapshot()
    {
        UndoSnapshot = CloneStyle(Style);
    }

    private async Task ToggleFavoriteAsync(string id)
    {
        if (FavoritePresets.Contains(id))
        {
            FavoritePresets.Remove(id);
        }
        else
        {
            FavoritePresets.Add(id);
        }

        try
        {
            await JS.InvokeVoidAsync("lpFramePresets.saveFavorites", FavoritePresets.ToArray());
        }
        catch
        {
        }
    }

    private bool IsFavorite(string id) => FavoritePresets.Contains(id);

    private static string BuildRgba(string color, int opacity)
    {
        var trimmed = color.Trim();
        if (!HexRegex.IsMatch(trimmed))
        {
            return color;
        }

        var hex = trimmed.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }

        var r = Convert.ToInt32(hex.Substring(0, 2), 16);
        var g = Convert.ToInt32(hex.Substring(2, 2), 16);
        var b = Convert.ToInt32(hex.Substring(4, 2), 16);
        var alpha = Math.Clamp(opacity / 100d, 0, 1);
        return $"rgba({r}, {g}, {b}, {alpha:0.##})";
    }

    private static string ResolveShadow(string? shadow)
    {
        return shadow switch
        {
            "sm" => "0 4px 10px rgba(15, 23, 42, 0.12)",
            "md" => "0 10px 24px rgba(15, 23, 42, 0.18)",
            "lg" => "0 18px 38px rgba(15, 23, 42, 0.22)",
            "none" or "off" or null or "" => "none",
            _ => shadow
        };
    }

    private void MarkCustomIfNeeded()
    {
        if (IsApplyingPreset)
        {
            return;
        }

        Style.PresetKey = null;
    }

    private static FrameStyle CloneStyle(FrameStyle style)
    {
        return new FrameStyle
        {
            Type = style.Type,
            BackgroundColor = style.BackgroundColor,
            BackgroundOpacity = style.BackgroundOpacity,
            BorderColor = style.BorderColor,
            BorderWidth = style.BorderWidth,
            BorderStyle = style.BorderStyle,
            BorderRadius = style.BorderRadius,
            BorderGradientColorA = style.BorderGradientColorA,
            BorderGradientColorB = style.BorderGradientColorB,
            BorderGradientAngle = style.BorderGradientAngle,
            ShadowLevel = style.ShadowLevel,
            PaddingPreset = style.PaddingPreset,
            PaddingX = style.PaddingX,
            PaddingY = style.PaddingY,
            MaxWidthPx = style.MaxWidthPx,
            Centered = style.Centered,
            HeaderBackgroundColor = style.HeaderBackgroundColor,
            HeaderTextColor = style.HeaderTextColor,
            HeaderFontSizePx = style.HeaderFontSizePx,
            HeaderFontFamily = style.HeaderFontFamily,
            HeaderHeightPx = style.HeaderHeightPx,
            HeaderRadiusTop = style.HeaderRadiusTop,
            BodyFontSizePx = style.BodyFontSizePx,
            BodyFontFamily = style.BodyFontFamily,
            CornerDecoration = CloneCornerDecoration(style.CornerDecoration),
            PresetKey = style.PresetKey,
            AnimationTargets = CloneAnimationTargets(style.AnimationTargets)
        };
    }

    private static void CopyStyle(FrameStyle source, FrameStyle target)
    {
        target.Type = source.Type;
        target.BackgroundColor = source.BackgroundColor;
        target.BackgroundOpacity = source.BackgroundOpacity;
        target.BorderColor = source.BorderColor;
        target.BorderWidth = source.BorderWidth;
        target.BorderStyle = source.BorderStyle;
        target.BorderRadius = source.BorderRadius;
        target.BorderGradientColorA = source.BorderGradientColorA;
        target.BorderGradientColorB = source.BorderGradientColorB;
        target.BorderGradientAngle = source.BorderGradientAngle;
        target.ShadowLevel = source.ShadowLevel;
        target.PaddingPreset = source.PaddingPreset;
        target.PaddingX = source.PaddingX;
        target.PaddingY = source.PaddingY;
        target.MaxWidthPx = source.MaxWidthPx;
        target.Centered = source.Centered;
        target.HeaderBackgroundColor = source.HeaderBackgroundColor;
        target.HeaderTextColor = source.HeaderTextColor;
        target.HeaderFontSizePx = source.HeaderFontSizePx;
        target.HeaderFontFamily = source.HeaderFontFamily;
        target.HeaderHeightPx = source.HeaderHeightPx;
        target.HeaderRadiusTop = source.HeaderRadiusTop;
        target.BodyFontSizePx = source.BodyFontSizePx;
        target.BodyFontFamily = source.BodyFontFamily;
        target.CornerDecoration = CloneCornerDecoration(source.CornerDecoration);
        target.PresetKey = source.PresetKey;
        target.AnimationTargets = CloneAnimationTargets(source.AnimationTargets);
    }

    private static Dictionary<string, FrameAnimationTargetSetting> CloneAnimationTargets(Dictionary<string, FrameAnimationTargetSetting> source)
    {
        var result = new Dictionary<string, FrameAnimationTargetSetting>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            result[pair.Key] = new FrameAnimationTargetSetting
            {
                PresetId = pair.Value.PresetId,
                DurationMs = pair.Value.DurationMs,
                DelayMs = pair.Value.DelayMs,
                Easing = pair.Value.Easing,
                Trigger = pair.Value.Trigger,
                Loop = pair.Value.Loop,
                Enabled = pair.Value.Enabled,
                SpDurationRate = pair.Value.SpDurationRate
            };
        }

        return result;
    }

    private static CornerDecorationSet? CloneCornerDecoration(CornerDecorationSet? set)
    {
        if (set is null)
        {
            return null;
        }

        return new CornerDecorationSet
        {
            TopLeft = CloneCornerDecoration(set.TopLeft),
            TopRight = CloneCornerDecoration(set.TopRight),
            BottomLeft = CloneCornerDecoration(set.BottomLeft),
            BottomRight = CloneCornerDecoration(set.BottomRight)
        };
    }

    private static CornerDecoration? CloneCornerDecoration(CornerDecoration? deco)
    {
        if (deco is null)
        {
            return null;
        }

        return new CornerDecoration
        {
            ImagePath = deco.ImagePath,
            SizePx = deco.SizePx,
            OffsetX = deco.OffsetX,
            OffsetY = deco.OffsetY,
            RotateDeg = deco.RotateDeg,
            Opacity = deco.Opacity,
            FlipX = deco.FlipX,
            FlipY = deco.FlipY,
            ZIndex = deco.ZIndex,
            Inside = deco.Inside
        };
    }

    private static bool IsInvalidColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !HexRegex.IsMatch(value.Trim());
    }
}
