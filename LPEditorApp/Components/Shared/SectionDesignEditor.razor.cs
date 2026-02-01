/* SectionDesignEditor removed */
#if false
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using LPEditorApp.Models;
using System.Text.RegularExpressions;

namespace LPEditorApp.Components.Shared;

public partial class SectionDesignEditor : ComponentBase
{
    private const string TypeSimple = "simple";
    private const string TypeColor = "color";
    private const string TypeGradient = "gradient";
    private const string TypeImage = "image";
    private const string TypeEmphasis = "emphasis";

    private const string KeyBackgroundColor = "BackgroundColor";
    private const string KeyBackgroundOpacity = "BackgroundOpacity";
    private const string KeyGradientA = "GradientColorA";
    private const string KeyGradientB = "GradientColorB";
    private const string KeyGradientDirection = "GradientDirection";
    private const string KeyImageUrl = "ImageUrl";
    private const string KeyOverlayColor = "OverlayColor";
    private const string KeyOverlayOpacity = "OverlayOpacity";
    private const string KeyBorderRadius = "BorderRadius";
    private const string KeyPaddingX = "PaddingX";
    private const string KeyPaddingY = "PaddingY";
    private const string KeyMarginBottom = "MarginBottom";
    private const string KeyBorderColor = "BorderColor";
    private const string KeyBorderWidth = "BorderWidth";
    private const string KeyAccentColor = "AccentColor";
    private const string KeyAccentHeight = "AccentHeight";

    [Parameter] public string SelectedSectionKey { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SelectedSectionKeyChanged { get; set; }
    [Parameter] public IReadOnlyList<KeyValuePair<string, string>> SectionOptions { get; set; } = Array.Empty<KeyValuePair<string, string>>();
    [Parameter] public SectionDesignModel Design { get; set; } = new();
    [Parameter] public IReadOnlyList<string> RecentColors { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback<string> OnColorUsed { get; set; }
    [Parameter] public EventCallback OnDesignChanged { get; set; }
    [Parameter] public EventCallback OnReset { get; set; }
    [Parameter] public EventCallback OnHighlight { get; set; }

    private static readonly IReadOnlyList<string> RecommendedColors = new[]
    {
        "#ffffff", "#f8fafc", "#e2e8f0", "#dbeafe", "#fef3c7", "#fde2e2", "#ede9fe", "#ecfccb", "#cffafe", "#fee2e2"
    };

    private static readonly IReadOnlyList<KeyValuePair<string, string>> ShadowOptions = new[]
    {
        new KeyValuePair<string, string>("off", "Off"),
        new KeyValuePair<string, string>("sm", "Sm"),
        new KeyValuePair<string, string>("md", "Md"),
        new KeyValuePair<string, string>("lg", "Lg")
    };

    private static readonly IReadOnlyList<KeyValuePair<string, string>> PatternOptions = new[]
    {
        new KeyValuePair<string, string>("off", "Off"),
        new KeyValuePair<string, string>("dots", "Dots"),
        new KeyValuePair<string, string>("stripes", "Stripes")
    };

    private static readonly IReadOnlyList<KeyValuePair<string, string>> AnimationOptions = new[]
    {
        new KeyValuePair<string, string>("off", "Off"),
        new KeyValuePair<string, string>("fadein", "FadeIn")
    };

    private static string GetChipClass(bool isActive)
    {
        return isActive ? "lp-chip is-active" : "lp-chip";
    }

    private static string GetOptionCardClass(bool isSelected)
    {
        return isSelected ? "lp-option-card is-selected" : "lp-option-card";
    }

    private static string GetSwatchStyle(string color)
    {
        return $"background:{color}";
    }

    private static string GetColorPickerValue(string? value)
    {
        return IsValidHex(value) ? value! : "#ffffff";
    }

    // SectionDesignEditor は削除済み
    private Task OnOverlayColorInput(ChangeEventArgs e) => OnColorInput(KeyOverlayColor, e);
    private Task OnOverlayOpacityInput(ChangeEventArgs e) => OnOpacityInput(KeyOverlayOpacity, e);

    private Task OnBackgroundColorChanged(string? value) => OnColorChanged(KeyBackgroundColor, value);
    private Task OnBackgroundOpacityChanged(double? value) => SetOpacity(KeyBackgroundOpacity, value);

    private Task OnBackgroundColorInput(ChangeEventArgs e) => OnColorInput(KeyBackgroundColor, e);
    private Task OnBackgroundOpacityInput(ChangeEventArgs e) => OnOpacityInput(KeyBackgroundOpacity, e);

    private Task OnBorderRadiusChanged(int? value) => SetInt(KeyBorderRadius, value);
    private Task OnPaddingXChanged(int? value) => SetInt(KeyPaddingX, value);
    private Task OnPaddingYChanged(int? value) => SetInt(KeyPaddingY, value);
    private Task OnMarginBottomChanged(int? value) => SetInt(KeyMarginBottom, value);

    private Task OnBorderRadiusInput(ChangeEventArgs e) => OnIntInput(KeyBorderRadius, e, 0, 48);
    private Task OnPaddingXInput(ChangeEventArgs e) => OnIntInput(KeyPaddingX, e, 0, 64);
    private Task OnPaddingYInput(ChangeEventArgs e) => OnIntInput(KeyPaddingY, e, 0, 64);
    private Task OnMarginBottomInput(ChangeEventArgs e) => OnIntInput(KeyMarginBottom, e, 0, 64);

    private Task OnBorderColorChanged(string? value) => OnColorChanged(KeyBorderColor, value);
    private Task OnBorderWidthChanged(int? value) => SetInt(KeyBorderWidth, value);

    private Task OnBorderColorInput(ChangeEventArgs e) => OnColorInput(KeyBorderColor, e);
    private Task OnBorderWidthInput(ChangeEventArgs e) => OnIntInput(KeyBorderWidth, e, 0, 6);

    private Task OnAccentColorChanged(string? value) => OnColorChanged(KeyAccentColor, value);
    private Task OnAccentHeightChanged(int? value) => SetInt(KeyAccentHeight, value);

    private Task OnAccentColorInput(ChangeEventArgs e) => OnColorInput(KeyAccentColor, e);
    private Task OnAccentHeightInput(ChangeEventArgs e) => OnIntInput(KeyAccentHeight, e, 2, 12);

    private async Task SetType(string type)
    {
        Design.Type = type;
        switch (type)
        {
            case "simple":
                Design.BackgroundColor = "#ffffff";
                Design.BackgroundOpacity = 1;
                Design.ShadowLevel = "off";
                Design.BorderWidth = 0;
                Design.PatternType = "off";
                break;
            case "color":
                Design.BackgroundColor ??= "#ffffff";
                Design.BackgroundOpacity ??= 1;
                Design.ShadowLevel = "md";
                Design.BorderWidth ??= 1;
                break;
            case "gradient":
                Design.GradientColorA ??= "#e0f2fe";
                Design.GradientColorB ??= "#ffffff";
                Design.ShadowLevel = "md";
                Design.BorderWidth ??= 1;
                break;
            case "image":
                Design.OverlayColor ??= "#111827";
                Design.OverlayOpacity ??= 0.35;
                Design.ShadowLevel = "md";
                break;
            case "emphasis":
                Design.BackgroundColor ??= "#ffffff";
                Design.BorderWidth = 3;
                Design.ShadowLevel = "lg";
                Design.AccentLineEnabled = true;
                Design.AccentHeight ??= 6;
                break;
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnColorChanged(string target, string? value)
    {
        switch (target)
        {
            case "BackgroundColor": Design.BackgroundColor = value; break;
            case "GradientColorA": Design.GradientColorA = value; break;
            case "GradientColorB": Design.GradientColorB = value; break;
            case "OverlayColor": Design.OverlayColor = value; break;
            case "BorderColor": Design.BorderColor = value; break;
            case "AccentColor": Design.AccentColor = value; break;
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            await OnColorUsed.InvokeAsync(value);
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetText(string target, string? value)
    {
        switch (target)
        {
            case "ImageUrl": Design.ImageUrl = value; break;
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetOpacity(string target, double? value)
    {
        var clamped = value.HasValue ? Math.Max(0, Math.Min(1, value.Value)) : (double?)null;
        switch (target)
        {
            case "BackgroundOpacity": Design.BackgroundOpacity = clamped; break;
            case "OverlayOpacity": Design.OverlayOpacity = clamped; break;
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetInt(string target, int? value)
    {
        switch (target)
        {
            case "GradientDirection": Design.GradientDirection = value; break;
            case "BorderRadius": Design.BorderRadius = value; break;
            case "PaddingX": Design.PaddingX = value; break;
            case "PaddingY": Design.PaddingY = value; break;
            case "MarginBottom": Design.MarginBottom = value; break;
            case "BorderWidth": Design.BorderWidth = value; break;
            case "AccentHeight": Design.AccentHeight = value; break;
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnPaddingPresetChanged()
    {
        if (!string.Equals(Design.PaddingPreset, "custom", StringComparison.OrdinalIgnoreCase))
        {
            var preset = Design.PaddingPreset ?? "md";
            (Design.PaddingX, Design.PaddingY) = preset switch
            {
                "sm" => (16, 16),
                "lg" => (32, 28),
                _ => (24, 24)
            };
        }
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnDesignChangedAsync()
    {
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnColorChangedAfter(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            await OnColorUsed.InvokeAsync(value);
        }

        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetShadow(string value)
    {
        Design.ShadowLevel = value;
        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetPattern(string value)
    {
        Design.PatternType = value;
        await OnDesignChanged.InvokeAsync();
    }

    private async Task SetAnimation(string value)
    {
        Design.Animation = value;
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnAccentToggle(ChangeEventArgs e)
    {
        Design.AccentLineEnabled = e.Value is bool b && b;
        await OnDesignChanged.InvokeAsync();
    }

    private async Task ApplyPaletteColor(string color)
    {
        if (IsType("gradient"))
        {
            Design.GradientColorA = color;
        }
        else if (IsType("image"))
        {
            Design.OverlayColor = color;
        }
        else
        {
            Design.BackgroundColor = color;
        }

        await OnColorUsed.InvokeAsync(color);
        await OnDesignChanged.InvokeAsync();
    }

    private async Task OnColorInput(string target, ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        await OnColorChanged(target, value);
    }

    private async Task OnOpacityInput(string target, ChangeEventArgs e)
    {
        double? value = null;
        if (double.TryParse(e.Value?.ToString(), out var parsed))
        {
            value = parsed / 100d;
        }
        await SetOpacity(target, value);
    }

    private async Task OnIntInput(string target, ChangeEventArgs e, int min, int max)
    {
        int? value = null;
        if (int.TryParse(e.Value?.ToString(), out var parsed))
        {
            value = Math.Clamp(parsed, min, max);
        }
        await SetInt(target, value);
    }
#endif
