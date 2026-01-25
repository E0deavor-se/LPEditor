using System.Net;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public class PreviewService
{
    public async Task<string> GenerateHtmlAsync(
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]>? imageOverrides,
        bool embedImages)
    {
        if (!template.Files.TryGetValue("index.html", out var indexFile))
        {
            indexFile = template.Files
                .FirstOrDefault(pair => pair.Key.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase))
                .Value;
        }

        if (indexFile is null)
        {
            throw new InvalidOperationException("index.html がテンプレートに見つかりません");
        }

        var html = System.Text.Encoding.UTF8.GetString(indexFile.Data);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        ApplyMeta(document, content);
        EnsureViewportMeta(document);
        if (embedImages)
        {
            EnsureBaseHref(document, "/");
        }
        EnsureEmphasisStyle(document);
        ApplyHeader(document, content, template, imageOverrides, embedImages);
        ApplyHero(document, content, template, imageOverrides, embedImages);
        ApplySections(document, content, template, imageOverrides, embedImages);
        ApplySectionFonts(document, content);
        ApplySectionTextAligns(document, content);
        EnsureCampaignEndedOverlay(document, content);
        EnsureRankingStyle(document, content);
        EnsureStoreSearchStyle(document, content);
        EnsureStoreSearchScript(document, content);
        EnsureSectionSpacingStyle(document);
        EnsurePreviewHighlightStyle(document);
        EnsureDecoZIndexStyle(document);
        EnsureDecoOverflowStyle(document);
        EnsureMobileOptimizeStyle(document, content);
        if (embedImages)
        {
            InlineStyles(document, template, content, imageOverrides, embedImages);
            InlineScripts(document, template, content);
        }

        ApplyCampaignStyle(document, content);
        EnsureBackgroundImageOnPage(document, content);
        ApplySectionBackgrounds(document, content);
        ApplySectionStyles(document, content);
        ApplySectionDecorations(document, content);
        ApplySectionAnimations(document, content);
        EnsureSectionAnimationStyle(document);
        EnsureSectionAnimationScript(document);
        EnsureDecorationStyle(document);
        ReplaceCountdownEndInScripts(document, content);
        EmbedImageSources(document, template, content, imageOverrides, embedImages);
        EnsurePageEffectsStyle(document, content);
        EnsurePageEffectsScript(document, content);
        ApplyPageEffects(document, content);

        return document.DocumentElement.OuterHtml;
    }

    private static void EnsureMobileOptimizeStyle(IDocument document, ContentModel content)
    {
        var style = content.CampaignStyle;
        if (!style.MobileAutoPadding && !style.MobileAutoFont)
        {
            return;
        }

        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-mobile-opt='true']") is not null)
        {
            return;
        }

        var rules = new List<string>();

        if (style.MobileAutoPadding)
        {
                        rules.Add(@"@media (max-width: 768px) {
    .section-group, section { padding-left: 12px !important; padding-right: 12px !important; }
    .section-group { margin: 0 !important; }
    .campaign__box, .campaign__subBox, .store-search-section .store-search-body, .ranking-section { padding: 16px 12px !important; }
    .ranking-section .ranking-table { margin-top: 10px !important; }
}");
        }

        if (style.MobileAutoFont)
        {
            rules.Add(@"@media (max-width: 768px) {
  html { font-size: 15px; }
  body { font-size: 15px; line-height: 1.65; }
  h1 { font-size: 1.6rem; }
  h2 { font-size: 1.35rem; }
  h3 { font-size: 1.15rem; }
  .campaign__text, .campaign__subBox li, .c-list li { font-size: 0.95rem; }
  .ranking-section .ranking-table th, .ranking-section .ranking-table td { font-size: 0.9rem; padding: 6px 8px; }
  .store-search-section .store-search-card { font-size: 0.9rem; }
}");
        }

        if (rules.Count == 0)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-mobile-opt", "true");
        styleTag.TextContent = string.Join(Environment.NewLine, rules);
        head.AppendChild(styleTag);
    }

    private static void EnsureSectionSpacingStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-section-spacing='true']") is not null)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-section-spacing", "true");
        styleTag.TextContent = @"
    .lp-canvas .section-group { margin: 0 !important; padding: 32px 0 !important; }
    .lp-canvas .section-group > section { margin: 0 !important; padding: 0 !important; }
    .lp-canvas .section-group > section > *:first-child { margin-top: 0 !important; }
    .lp-canvas .section-group p:first-child, .lp-canvas .section-group ul:first-child, .lp-canvas .section-group ol:first-child { margin-top: 0 !important; }
    .lp-canvas .section-group .campaign__block,
    .lp-canvas .section-group .campaign__subBox,
    .lp-canvas .section-group .store-search-section .store-search-body,
    .lp-canvas .section-group .ranking-section { margin: 0 auto !important; }
    ";
        head.AppendChild(styleTag);
    }

    private static void EnsurePreviewHighlightStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-preview-highlight='true']") is not null)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-preview-highlight", "true");
        styleTag.TextContent = @"
.lp-highlight { outline: 2px solid rgba(14,165,233,0.85); box-shadow: 0 0 0 4px rgba(14,165,233,0.18); transition: box-shadow 0.3s ease; }
.lp-highlight-background { outline-color: rgba(249,115,22,0.85); box-shadow: 0 0 0 4px rgba(249,115,22,0.2); }
.lp-highlight-decor { outline-color: rgba(99,102,241,0.85); box-shadow: 0 0 0 4px rgba(99,102,241,0.2); }
.lp-final-mode [class*='guide'], .lp-final-mode .guide, .lp-final-mode .slot-guide { display: none !important; }
";
        head.AppendChild(styleTag);
    }

    private static void EnsureDecoZIndexStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-deco-z='true']") is not null)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-deco-z", "true");
        styleTag.TextContent = @"
/* decoration (deco) elements: bring to front */
[class*='deco'],
[class*='decoration'],
.deco,
.decoration,
.deco-item,
.decoration-item,
.deco-img,
.decoration-img,
img[class*='deco'],
img[class*='decoration'] { z-index: 50 !important; }
";
        head.AppendChild(styleTag);
    }

    private static void EnsureDecoOverflowStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-deco-overflow='true']") is not null)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-deco-overflow", "true");
        styleTag.TextContent = @"
/* allow decoration to overflow section/card wrappers */
.lp-canvas,
.section-group,
.section-group > section,
.campaign,
.l-page-contents,
.campaign__block,
.campaign__box,
.campaign__subBox { overflow: visible !important; }

/* keep rounded corner clipping on inner cards only */
.campaign__box > .campaign__inner,
.campaign__subBox > .campaign__inner {
  overflow: hidden !important;
  border-radius: inherit;
}
";
        head.AppendChild(styleTag);
    }

        private static void EnsureBackgroundImageOnPage(IDocument document, ContentModel content)
        {
                var head = document.Head;
                var body = document.Body;
                if (head is null || body is null)
                {
                        return;
                }

                body.ClassList.Add("lp-page-bg");
                var wrapper = EnsureBackgroundWrapper(document, content);

                var background = content.LpBackground ?? new LpBackgroundModel();
                var setting = BackgroundSettingMapper.FromPage(background);

                var styleTag = document.QuerySelector("style[data-bg-transfer='true']") as IElement;
                if (styleTag is null)
                {
                        styleTag = document.CreateElement("style");
                        styleTag.SetAttribute("data-bg-transfer", "true");
                        head.AppendChild(styleTag);
                }

                var rules = new StringBuilder();
                var includeMediaImage = !BackgroundRenderService.UseMediaLayer(setting);
                var baseRule = BackgroundStyleService.BuildRule(setting, "html, body", includeMediaImage);
                var wrapperRule = BackgroundStyleService.BuildRule(setting, ".lp-canvas, .page, .lp-wrapper, .l-wrapper", includeMediaImage);
                if (!string.IsNullOrWhiteSpace(baseRule))
                {
                    rules.AppendLine(baseRule);
                }
                if (!string.IsNullOrWhiteSpace(wrapperRule))
                {
                    rules.AppendLine(wrapperRule);
                }

                if (background.TransparentSections)
                {
                    rules.AppendLine(".lp-canvas .section-group, .lp-canvas section, .lp-canvas .campaign__block, .lp-canvas .campaign__box, .lp-canvas .campaign__subBox, .lp-canvas .ranking-section, .lp-canvas .store-search-section .store-search-body { background: transparent !important; }");
                }

                styleTag.TextContent = rules.ToString();

                ApplyPageBackgroundMediaLayers(document, wrapper, setting);
        }

            private static void ApplyPageBackgroundMediaLayers(IDocument document, IElement wrapper, BackgroundSetting setting)
            {
                var existing = wrapper.QuerySelector(".lp-bg-stage") as IElement;
                if (!BackgroundRenderService.UseMediaLayer(setting))
                {
                    existing?.Remove();
                    return;
                }

                var stage = existing ?? document.CreateElement("div");
                stage.ClassList.Add("lp-bg-stage");
                stage.SetAttribute("style", "position:fixed;inset:0;z-index:-1;overflow:hidden;");

                var media = stage.QuerySelector(".lp-bg-media") as IElement;
                var overlay = stage.QuerySelector(".lp-bg-overlay") as IElement;

                if (media is null)
                {
                    media = document.CreateElement("div");
                    media.ClassList.Add("lp-bg-media");
                    stage.AppendChild(media);
                }

                if (overlay is null)
                {
                    overlay = document.CreateElement("div");
                    overlay.ClassList.Add("lp-bg-overlay");
                    stage.AppendChild(overlay);
                }

                var sourceType = BackgroundRenderService.ResolveSourceType(setting);
                if (sourceType == "video" && !string.IsNullOrWhiteSpace(setting.VideoUrl))
                {
                    var video = media.QuerySelector("video") as IElement;
                    if (video is null)
                    {
                        foreach (var child in media.Children.ToList())
                        {
                            child.Remove();
                        }
                        video = document.CreateElement("video");
                        video.SetAttribute("autoplay", string.Empty);
                        video.SetAttribute("muted", string.Empty);
                        video.SetAttribute("loop", string.Empty);
                        video.SetAttribute("playsinline", string.Empty);
                        media.AppendChild(video);
                    }

                    video.SetAttribute("style", "width:100%;height:100%;object-fit:cover;" + BackgroundRenderService.BuildFilterStyle(setting.Effects));
                    video.SetAttribute("src", setting.VideoUrl);
                    if (!string.IsNullOrWhiteSpace(setting.VideoPoster))
                    {
                        video.SetAttribute("poster", setting.VideoPoster);
                    }
                }
                else
                {
                    foreach (var child in media.Children.ToList())
                    {
                        child.Remove();
                    }
                    media.SetAttribute("style", "position:absolute;inset:0;" + BackgroundRenderService.BuildMediaStyle(setting) + BackgroundRenderService.BuildFilterStyle(setting.Effects));
                }

                overlay.SetAttribute("style", "position:absolute;inset:0;" + BackgroundRenderService.BuildOverlayStyle(setting.Effects));

                if (existing is null)
                {
                    wrapper.AppendChild(stage);
                }
            }

            private static void ApplySectionBackgrounds(IDocument document, ContentModel content)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                var styleTag = document.QuerySelector("style[data-section-bg='true']") as IElement;
                if (styleTag is null)
                {
                    styleTag = document.CreateElement("style");
                    styleTag.SetAttribute("data-section-bg", "true");
                    head.AppendChild(styleTag);
                }

                var rules = new StringBuilder();
                rules.AppendLine("header, .header { background-color: #ffffff !important; background-image: none !important; }");
                rules.AppendLine(".section-group[data-section='conditions-contact-banners'], section.conditions, .banner-head, a.banner, .magazine { background-color: #ffffff !important; }");
                rules.AppendLine("section.contact { background-color: #000000 !important; background-image: none !important; }");
                rules.AppendLine("@media (max-width: 768px) { .section-group[data-section='campaign-content'], .section-group[data-section='coupon-period'], .section-group[data-section='coupon-notes'], .section-group[data-section='ranking'], .section-group[data-section^='store-search'], .section-group.custom-section, .section-group[data-section='campaign-content'] section, .section-group[data-section='coupon-period'] section, .section-group[data-section='coupon-notes'] section, .section-group[data-section='ranking'] section, .section-group[data-section^='store-search'] section, .section-group.custom-section section, .section-group[data-section='campaign-content'] .l-page-contents, .section-group[data-section='coupon-period'] .l-page-contents, .section-group[data-section='coupon-notes'] .l-page-contents, .section-group[data-section='ranking'] .l-page-contents, .section-group[data-section^='store-search'] .l-page-contents, .section-group.custom-section .l-page-contents, .section-group[data-section='campaign-content'] .campaign__block, .section-group[data-section='coupon-period'] .campaign__block, .section-group[data-section='coupon-notes'] .campaign__block, .section-group[data-section='ranking'] .campaign__block, .section-group[data-section^='store-search'] .campaign__block, .section-group.custom-section .campaign__block { background-color: transparent !important; background-image: none !important; } }");

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (content.SectionBackgrounds is not null)
                {
                    foreach (var key in content.SectionBackgrounds.Keys)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            keys.Add(key);
                        }
                    }
                }
                if (content.SectionStyles is not null)
                {
                    foreach (var key in content.SectionStyles.Keys)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            keys.Add(key);
                        }
                    }
                }

                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var rawKey = group.GetAttribute("data-section");
                    var normalizedKey = NormalizeSectionKey(rawKey);
                    if (!string.IsNullOrWhiteSpace(normalizedKey))
                    {
                        group.SetAttribute("data-section-key", normalizedKey);
                    }
                }

                foreach (var key in keys)
                {
                    if (!IsEditorManagedSectionKey(key, content))
                    {
                        continue;
                    }

                    var settings = ResolveSectionBackgroundSettings(content, key);
                    if (settings is null)
                    {
                        continue;
                    }

                    var sectionKey = NormalizeSectionKey(key);
                    var selector = $".lp-canvas .section-group[data-section-key='{sectionKey}'], .lp-canvas .section-group[data-section-key='{sectionKey}'] > section";
                    var rule = BuildSectionBackgroundRule(settings, selector);
                    if (!string.IsNullOrWhiteSpace(rule))
                    {
                        rules.AppendLine(rule);
                    }
                }

                styleTag.TextContent = rules.ToString();

                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var normalizedKey = NormalizeSectionKey(group.GetAttribute("data-section"));
                    if (string.IsNullOrWhiteSpace(normalizedKey))
                    {
                        continue;
                    }

                    var section = group.Children.FirstOrDefault(child => string.Equals(child.TagName, "SECTION", StringComparison.OrdinalIgnoreCase));
                    var settings = FindEditedBackgroundSettingsByNormalizedKey(content, normalizedKey);

                    if (settings is null)
                    {
                        ClearInlineBackground(group);
                        if (section is not null)
                        {
                            ClearInlineBackground(section);
                        }
                        continue;
                    }

                    ApplyInlineBackground(group, settings);
                    if (section is not null)
                    {
                        ApplyInlineBackground(section, settings);
                    }
                }
            }

            private static SectionBackgroundSettings? ResolveSectionBackgroundSettings(ContentModel content, string key)
            {
                if (content.SectionStyles is not null
                    && content.SectionStyles.TryGetValue(key, out var style)
                    && style?.Background is not null
                    && IsEditedSectionBackground(style.Background))
                {
                    return style.Background;
                }

                if (content.SectionBackgrounds is not null
                    && content.SectionBackgrounds.TryGetValue(key, out var settings)
                    && settings is not null
                    && IsEditedSectionBackground(settings))
                {
                    return settings;
                }

                return null;
            }

            private static SectionBackgroundSettings? FindEditedBackgroundSettingsByNormalizedKey(ContentModel content, string normalizedKey)
            {
                if (content.SectionStyles is not null)
                {
                    foreach (var pair in content.SectionStyles)
                    {
                        if (NormalizeSectionKey(pair.Key) != normalizedKey)
                        {
                            continue;
                        }

                        var background = pair.Value?.Background;
                        if (background is not null && IsEditedSectionBackground(background))
                        {
                            return background;
                        }
                    }
                }

                if (content.SectionBackgrounds is not null)
                {
                    foreach (var pair in content.SectionBackgrounds)
                    {
                        if (NormalizeSectionKey(pair.Key) != normalizedKey)
                        {
                            continue;
                        }

                        if (pair.Value is not null && IsEditedSectionBackground(pair.Value))
                        {
                            return pair.Value;
                        }
                    }
                }

                return null;
            }

            private static void ApplyInlineBackground(IElement element, SectionBackgroundSettings settings)
            {
                var rules = BuildInlineBackgroundRules(settings);
                if (rules.Count == 0)
                {
                    return;
                }

                var current = element.GetAttribute("style") ?? string.Empty;
                var addition = string.Join(" ", rules);
                if (!string.IsNullOrWhiteSpace(current) && !current.TrimEnd().EndsWith(";", StringComparison.Ordinal))
                {
                    current += ";";
                }

                element.SetAttribute("style", string.Concat(current, addition));
            }

            private static void ClearInlineBackground(IElement element)
            {
                var current = element.GetAttribute("style");
                if (string.IsNullOrWhiteSpace(current))
                {
                    return;
                }

                var cleaned = Regex.Replace(current, "(?i)background(-color|-image|-repeat|-position|-size|-attachment)?\\s*:[^;]+;?", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    element.RemoveAttribute("style");
                    return;
                }

                element.SetAttribute("style", cleaned);
            }

            private static List<string> BuildInlineBackgroundRules(SectionBackgroundSettings settings)
            {
                var dto = BackgroundSettingMapper.FromSection(settings);
                return BackgroundStyleService.BuildInlineRules(dto, includeImportant: true);
            }

            private static bool IsEditedSectionBackground(SectionBackgroundSettings settings)
            {
                var mode = settings.Mode?.Trim().ToLowerInvariant() ?? "inherit";
                if (mode == "inherit")
                {
                    return false;
                }

                if (mode == "preset")
                {
                    return !string.IsNullOrWhiteSpace(settings.Preset?.PresetKey);
                }

                if (mode == "color")
                {
                    return !string.IsNullOrWhiteSpace(SanitizeCssColor(settings.Color));
                }

                if (mode == "gradient")
                {
                    return !string.IsNullOrWhiteSpace(SanitizeCssColor(settings.GradientColorA))
                           && !string.IsNullOrWhiteSpace(SanitizeCssColor(settings.GradientColorB));
                }

                if (mode == "image")
                {
                    return !string.IsNullOrWhiteSpace(settings.ImageUrl);
                }

                return false;
            }

            private static void ApplySectionStyles(IDocument document, ContentModel content)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                var styleTag = document.QuerySelector("style[data-section-style='true']") as IElement;
                if (styleTag is null)
                {
                    styleTag = document.CreateElement("style");
                    styleTag.SetAttribute("data-section-style", "true");
                    head.AppendChild(styleTag);
                }

                var rules = new StringBuilder();

                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var key = group.GetAttribute("data-section");
                    if (string.IsNullOrWhiteSpace(key) || !IsEditorManagedSectionKey(key, content))
                    {
                        continue;
                    }

                    var style = ResolveSectionStyle(content, key);
                    if (style is null)
                    {
                        continue;
                    }

                    var selector = $".lp-content-wrap .section-group[data-section='{key}']";
                    var rule = BuildSectionStyleRule(style, selector);
                    if (!string.IsNullOrWhiteSpace(rule))
                    {
                        rules.AppendLine(rule);
                    }

                    if (style.Divider is not null && style.Divider.Enabled)
                    {
                        var color = SanitizeCssColor(style.Divider.Color) ?? "#e5e7eb";
                        var thickness = style.Divider.Thickness is > 0 ? style.Divider.Thickness.Value : 1;
                        var lineStyle = string.IsNullOrWhiteSpace(style.Divider.Style) ? "solid" : style.Divider.Style;
                        var marginTop = style.Divider.MarginTop is >= 0 ? style.Divider.MarginTop.Value : 12;
                        var marginBottom = style.Divider.MarginBottom is >= 0 ? style.Divider.MarginBottom.Value : 12;
                        rules.AppendLine($"{selector}::before {{ content: ''; display: block; border-top: {thickness}px {lineStyle} {color}; margin: {marginTop}px 0 {marginBottom}px; }}");
                    }
                }

                styleTag.TextContent = rules.ToString();
            }

            private static SectionStyleModel? ResolveSectionStyle(ContentModel content, string key)
            {
                if (content.SectionStyles is not null
                    && content.SectionStyles.TryGetValue(key, out var style)
                    && style is not null)
                {
                    return style;
                }

                var background = ResolveSectionBackgroundSettings(content, key);
                if (background is null)
                {
                    return null;
                }

                return new SectionStyleModel
                {
                    Background = background
                };
            }

            private static string BuildSectionStyleRule(SectionStyleModel style, string selector)
            {
                var rules = new List<string>();

                var borderColor = SanitizeCssColor(style.BorderColor);
                if (!string.IsNullOrWhiteSpace(borderColor))
                {
                    var borderWidth = style.BorderWidth is > 0 ? style.BorderWidth.Value : 1;
                    var borderStyle = string.IsNullOrWhiteSpace(style.BorderStyle) ? "solid" : style.BorderStyle;
                    rules.Add($"border: {borderWidth}px {borderStyle} {borderColor} !important;");
                }

                if (style.Radius is > 0)
                {
                    rules.Add($"border-radius: {style.Radius}px !important;");
                }

                var shadow = ResolveSectionShadow(style.Shadow);
                if (!string.IsNullOrWhiteSpace(shadow))
                {
                    rules.Add($"box-shadow: {shadow} !important;");
                }

                if (style.PaddingTop is > 0)
                {
                    rules.Add($"padding-top: {style.PaddingTop}px !important;");
                }
                if (style.PaddingRight is > 0)
                {
                    rules.Add($"padding-right: {style.PaddingRight}px !important;");
                }
                if (style.PaddingBottom is > 0)
                {
                    rules.Add($"padding-bottom: {style.PaddingBottom}px !important;");
                }
                if (style.PaddingLeft is > 0)
                {
                    rules.Add($"padding-left: {style.PaddingLeft}px !important;");
                }

                if (rules.Count == 0)
                {
                    return string.Empty;
                }

                return $"{selector} {{ {string.Join(" ", rules)} }}";
            }

            private static string? ResolveSectionShadow(string value)
            {
                var normalized = value?.Trim().ToLowerInvariant();
                return normalized switch
                {
                    "sm" => "0 4px 10px rgba(15, 23, 42, 0.12)",
                    "md" => "0 10px 24px rgba(15, 23, 42, 0.18)",
                    "lg" => "0 18px 38px rgba(15, 23, 42, 0.22)",
                    "none" or "" or null => null,
                    _ => value
                };
            }

            private static void ApplySectionAnimations(IDocument document, ContentModel content)
            {
                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var key = group.GetAttribute("data-section");
                    if (string.IsNullOrWhiteSpace(key) || !IsEditorManagedSectionKey(key, content))
                    {
                        continue;
                    }

                    var animation = ResolveSectionAnimation(content, key);
                    if (animation is null || string.Equals(animation.Type, "none", StringComparison.OrdinalIgnoreCase))
                    {
                        group.RemoveAttribute("data-anim");
                        group.RemoveAttribute("data-anim-duration");
                        group.RemoveAttribute("data-anim-delay");
                        group.RemoveAttribute("data-anim-easing");
                        group.RemoveAttribute("data-anim-trigger");
                        group.RemoveAttribute("data-anim-repeat");
                        group.RemoveAttribute("data-anim-once");
                        group.ClassList.Remove("lp-animate");
                        continue;
                    }

                    group.SetAttribute("data-anim", animation.Type ?? "fade");
                    if (animation.Duration is > 0)
                    {
                        group.SetAttribute("data-anim-duration", animation.Duration.Value.ToString());
                    }
                    if (animation.Delay is >= 0)
                    {
                        group.SetAttribute("data-anim-delay", animation.Delay.Value.ToString());
                    }
                    if (!string.IsNullOrWhiteSpace(animation.Easing))
                    {
                        group.SetAttribute("data-anim-easing", animation.Easing);
                    }
                    if (!string.IsNullOrWhiteSpace(animation.Trigger))
                    {
                        group.SetAttribute("data-anim-trigger", animation.Trigger);
                    }
                    if (!string.IsNullOrWhiteSpace(animation.Repeat))
                    {
                        group.SetAttribute("data-anim-repeat", animation.Repeat);
                    }
                    group.SetAttribute("data-anim-once", animation.Once ? "true" : "false");
                    group.ClassList.Add("lp-animate");
                }
            }

            private static SectionAnimationModel? ResolveSectionAnimation(ContentModel content, string key)
            {
                if (content.SectionStyles is not null
                    && content.SectionStyles.TryGetValue(key, out var style)
                    && style?.SectionAnimation is not null)
                {
                    return style.SectionAnimation;
                }

                if (content.SectionAnimations is not null
                    && content.SectionAnimations.TryGetValue(key, out var animation))
                {
                    return animation;
                }

                return null;
            }

            private static void EnsureSectionAnimationStyle(IDocument document)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("style[data-section-anim-style='true']") is not null)
                {
                    return;
                }

                var style = document.CreateElement("style");
                style.SetAttribute("data-section-anim-style", "true");
                style.TextContent = @"
.lp-animate {
  opacity: 0;
  transform: translateY(20px);
  transition: opacity var(--lp-anim-duration, 600ms) var(--lp-anim-easing, ease),
              transform var(--lp-anim-duration, 600ms) var(--lp-anim-easing, ease);
  transition-delay: var(--lp-anim-delay, 0ms);
  will-change: opacity, transform;
}
.lp-animate.is-inview {
  opacity: 1;
  transform: none;
}
[data-anim-trigger='load'] { opacity: 1; transform: none; }
[data-anim-repeat='loop'] {
    animation-duration: var(--lp-anim-duration, 1600ms);
    animation-delay: var(--lp-anim-delay, 0ms);
    animation-timing-function: var(--lp-anim-easing, ease-in-out);
    animation-iteration-count: infinite;
    animation-direction: alternate;
}
[data-anim='fade'] { transform: none; }
[data-anim='slide-up'] { transform: translateY(24px); }
[data-anim='slide-down'] { transform: translateY(-24px); }
[data-anim='slide-left'] { transform: translateX(24px); }
[data-anim='slide-right'] { transform: translateX(-24px); }
[data-anim='zoom'] { transform: scale(0.96); }
[data-anim='pop'] { transform: scale(0.92); }

@keyframes lp-loop-fade { 0% { opacity: 0.6; } 100% { opacity: 1; } }
@keyframes lp-loop-slide-up { 0% { transform: translateY(12px); } 100% { transform: translateY(-4px); } }
@keyframes lp-loop-slide-down { 0% { transform: translateY(-12px); } 100% { transform: translateY(4px); } }
@keyframes lp-loop-slide-left { 0% { transform: translateX(12px); } 100% { transform: translateX(-4px); } }
@keyframes lp-loop-slide-right { 0% { transform: translateX(-12px); } 100% { transform: translateX(4px); } }
@keyframes lp-loop-zoom { 0% { transform: scale(0.98); } 100% { transform: scale(1.02); } }
@keyframes lp-loop-pop { 0% { transform: scale(0.94); } 100% { transform: scale(1); } }

[data-anim-repeat='loop'][data-anim='fade'] { animation-name: lp-loop-fade; }
[data-anim-repeat='loop'][data-anim='slide-up'] { animation-name: lp-loop-slide-up; }
[data-anim-repeat='loop'][data-anim='slide-down'] { animation-name: lp-loop-slide-down; }
[data-anim-repeat='loop'][data-anim='slide-left'] { animation-name: lp-loop-slide-left; }
[data-anim-repeat='loop'][data-anim='slide-right'] { animation-name: lp-loop-slide-right; }
[data-anim-repeat='loop'][data-anim='zoom'] { animation-name: lp-loop-zoom; }
[data-anim-repeat='loop'][data-anim='pop'] { animation-name: lp-loop-pop; }
";
                head.AppendChild(style);
            }

            private static void EnsureSectionAnimationScript(IDocument document)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("script[data-section-anim='true']") is not null)
                {
                    return;
                }

                var script = document.CreateElement("script");
                script.SetAttribute("data-section-anim", "true");
                script.TextContent = @"
(function(){
    var items = Array.prototype.slice.call(document.querySelectorAll('[data-anim]'));
  if (items.length === 0) return;

  function applyVars(el){
    var duration = el.getAttribute('data-anim-duration') || '600';
    var delay = el.getAttribute('data-anim-delay') || '0';
    var easing = el.getAttribute('data-anim-easing') || 'ease';
    el.style.setProperty('--lp-anim-duration', duration + 'ms');
    el.style.setProperty('--lp-anim-delay', delay + 'ms');
    el.style.setProperty('--lp-anim-easing', easing);
  }

    if (!('IntersectionObserver' in window)) {
        items.forEach(function(el){ applyVars(el); el.classList.add('is-inview'); });
    return;
  }

  var observer = new IntersectionObserver(function(entries){
    entries.forEach(function(entry){
      if (!entry.isIntersecting) return;
      var el = entry.target;
      el.classList.add('is-inview');
      if (el.getAttribute('data-anim-once') === 'true') {
        observer.unobserve(el);
      }
    });
  }, { threshold: 0.2 });

    items.forEach(function(el){
        applyVars(el);
        if (el.getAttribute('data-anim-trigger') === 'load') {
            el.classList.add('is-inview');
            return;
        }
        observer.observe(el);
    });
})();
";
                head.AppendChild(script);
            }

            private static void EnsureDecorationStyle(IDocument document)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("style[data-decor-style='true']") is not null)
                {
                    return;
                }

                var style = document.CreateElement("style");
                style.SetAttribute("data-decor-style", "true");
                style.TextContent = @"
.section-group { position: relative; }
.lp-decorations { position:absolute; inset:0; pointer-events:none; }
.lp-decor { position:absolute; opacity: var(--decor-opacity, 1); background: var(--decor-color, #1e1b4b); color:#fff; display:grid; place-items:center; font-size:0.75rem; font-weight:700; border-radius:999px; }
.lp-decor.layer-back { z-index:1; }
.lp-decor.layer-front { z-index:3; }
.lp-decor.pos-top { top: calc(8px + var(--decor-offset-y, 0px)); left:50%; transform: translateX(-50%); width: calc(var(--decor-size, 40px) * 3); height: var(--decor-size, 40px); }
.lp-decor.pos-bottom { bottom: calc(8px + var(--decor-offset-y, 0px)); left:50%; transform: translateX(-50%); width: calc(var(--decor-size, 40px) * 3); height: var(--decor-size, 40px); }
.lp-decor.pos-left { left: calc(8px + var(--decor-offset-x, 0px)); top:50%; transform: translateY(-50%); width: var(--decor-size, 40px); height: calc(var(--decor-size, 40px) * 2); }
.lp-decor.pos-right { right: calc(8px + var(--decor-offset-x, 0px)); top:50%; transform: translateY(-50%); width: var(--decor-size, 40px); height: calc(var(--decor-size, 40px) * 2); }
.lp-decor.pos-center { left:50%; top:50%; transform: translate(-50%,-50%); width: calc(var(--decor-size, 40px) * 2); height: var(--decor-size, 40px); }
.lp-decor.decor-divider { height: calc(var(--decor-size, 40px) / 6); border-radius:8px; }
.lp-decor.decor-pattern { width:100%; height:100%; opacity:0.15; background: transparent; background-image: radial-gradient(var(--decor-color, #1e1b4b) 1px, transparent 1px); background-size: 20px 20px; }
.lp-decor.decor-shape { border-radius:50%; }
";
                head.AppendChild(style);
            }

            private static void ApplySectionDecorations(IDocument document, ContentModel content)
            {
                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var key = group.GetAttribute("data-section");
                    if (string.IsNullOrWhiteSpace(key) || !IsEditorManagedSectionKey(key, content))
                    {
                        continue;
                    }

                    if (content.SectionStyles is null
                        || !content.SectionStyles.TryGetValue(key, out var style)
                        || style is null
                        || style.Decorations is null
                        || style.Decorations.Count == 0)
                    {
                        var existing = group.QuerySelector(".lp-decorations") as IElement;
                        existing?.Remove();
                        continue;
                    }

                    var container = group.QuerySelector(".lp-decorations") as IElement ?? document.CreateElement("div");
                    container.ClassName = "lp-decorations";
                    container.InnerHtml = string.Empty;
                    foreach (var layer in style.Decorations.Where(l => l.Enabled))
                    {
                        var decor = document.CreateElement("div");
                        var size = layer.SizePx ?? (layer.SizePreset == "s" ? 24 : layer.SizePreset == "l" ? 64 : 40);
                        var opacity = Math.Clamp(layer.OpacityPct ?? 100, 0, 100) / 100d;
                        var color = string.IsNullOrWhiteSpace(layer.Color) ? "#1e1b4b" : layer.Color;
                        var offsetX = layer.OffsetX ?? 0;
                        var offsetY = layer.OffsetY ?? 0;
                        decor.ClassName = $"lp-decor decor-{layer.Type} layer-{layer.Layer} pos-{layer.Position}";
                        decor.SetAttribute("style", $"--decor-size:{size}px;--decor-color:{color};--decor-opacity:{opacity};--decor-offset-x:{offsetX}px;--decor-offset-y:{offsetY}px;");
                        if (!string.IsNullOrWhiteSpace(layer.Label))
                        {
                            decor.TextContent = layer.Label;
                        }

                        if (layer.Animation is not null && !string.Equals(layer.Animation.Preset, "none", StringComparison.OrdinalIgnoreCase))
                        {
                            decor.SetAttribute("data-anim", layer.Animation.Preset ?? "fade");
                            if (layer.Animation.DurationMs is > 0)
                            {
                                decor.SetAttribute("data-anim-duration", layer.Animation.DurationMs.Value.ToString());
                            }
                            if (layer.Animation.DelayMs is >= 0)
                            {
                                decor.SetAttribute("data-anim-delay", layer.Animation.DelayMs.Value.ToString());
                            }
                            if (!string.IsNullOrWhiteSpace(layer.Animation.Easing))
                            {
                                decor.SetAttribute("data-anim-easing", layer.Animation.Easing);
                            }
                            if (!string.IsNullOrWhiteSpace(layer.Animation.Trigger))
                            {
                                decor.SetAttribute("data-anim-trigger", layer.Animation.Trigger);
                            }
                            if (!string.IsNullOrWhiteSpace(layer.Animation.Repeat))
                            {
                                decor.SetAttribute("data-anim-repeat", layer.Animation.Repeat);
                            }
                            decor.SetAttribute("data-anim-once", "true");
                            decor.ClassList.Add("lp-animate");
                        }

                        container.AppendChild(decor);
                    }

                    if (container.ParentElement != group)
                    {
                        group.AppendChild(container);
                    }
                }
            }

            private static void EnsurePageEffectsStyle(IDocument document, ContentModel content)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("style[data-page-effects='true']") is not null)
                {
                    return;
                }

                var style = document.CreateElement("style");
                style.SetAttribute("data-page-effects", "true");
                style.TextContent = @"
.lp-page-effects { position:fixed; inset:0; pointer-events:none; z-index:2; }
.lp-page-effects canvas { position:absolute; inset:0; width:100%; height:100%; }
.lp-page-effects .effects-noise { position:absolute; inset:0; opacity:0.08; mix-blend-mode:soft-light; background-size:160px 160px; }
.lp-page-effects .effects-gradient { position:absolute; inset:-20%; filter: blur(60px); opacity:0.35; animation: lp-gradient-drift 20s ease-in-out infinite alternate; }
@keyframes lp-gradient-drift { 0% { transform: translate(-10%, -10%); } 100% { transform: translate(10%, 10%); } }
";
                head.AppendChild(style);
            }

            private static void ApplyPageEffects(IDocument document, ContentModel content)
            {
                var body = document.Body;
                if (body is null)
                {
                    return;
                }

                var effects = content.PageEffects ?? new PageEffectsSetting();
                if (!effects.Enabled && !effects.Sparkle.Enabled && !effects.Noise.Enabled && !effects.GradientDrift.Enabled)
                {
                    var existing = body.QuerySelector(".lp-page-effects") as IElement;
                    existing?.Remove();
                    return;
                }

                var wrapper = body.QuerySelector(".lp-page-effects") as IElement ?? document.CreateElement("div");
                wrapper.ClassName = "lp-page-effects";
                wrapper.InnerHtml = string.Empty;

                if (effects.GradientDrift.Enabled)
                {
                    var gradient = document.CreateElement("div");
                    gradient.ClassName = "effects-gradient";
                    var colorA = string.IsNullOrWhiteSpace(effects.GradientDrift.ColorA) ? "#7c3aed" : effects.GradientDrift.ColorA;
                    var colorB = string.IsNullOrWhiteSpace(effects.GradientDrift.ColorB) ? "#0ea5e9" : effects.GradientDrift.ColorB;
                    var gradientOpacity = effects.GradientDrift.Intensity switch
                    {
                        "high" => 0.55,
                        "mid" => 0.4,
                        _ => 0.28
                    };
                    var duration = effects.GradientDrift.Speed is > 0
                        ? Math.Max(6, 22 - (effects.GradientDrift.Speed.Value / 5))
                        : 20;
                    gradient.SetAttribute("style", $"background: radial-gradient(circle at 20% 20%, {colorA}, transparent 60%), radial-gradient(circle at 80% 80%, {colorB}, transparent 60%); opacity:{gradientOpacity}; animation-duration:{duration}s;");
                    wrapper.AppendChild(gradient);
                }

                if (effects.Noise.Enabled)
                {
                    var noise = document.CreateElement("div");
                    noise.ClassName = "effects-noise";
                    var color = string.IsNullOrWhiteSpace(effects.Noise.Color) ? "#ffffff" : effects.Noise.Color;
                    var noiseOpacity = effects.Noise.Intensity switch
                    {
                        "high" => 0.18,
                        "mid" => 0.12,
                        _ => 0.08
                    };
                    var density = effects.Noise.Density ?? 0;
                    var size = Math.Max(80, 180 - density * 8);
                    noise.SetAttribute("style", $"background-image: repeating-linear-gradient(0deg, {color} 0, {color} 1px, transparent 1px, transparent 2px); opacity:{noiseOpacity}; background-size:{size}px {size}px;");
                    wrapper.AppendChild(noise);
                }

                if (effects.Sparkle.Enabled)
                {
                    var canvas = document.CreateElement("canvas");
                    canvas.SetAttribute("data-sparkle", "true");
                    canvas.SetAttribute("data-sparkle-intensity", effects.Sparkle.Intensity ?? "low");
                    canvas.SetAttribute("data-sparkle-color", effects.Sparkle.Color ?? "#ffffff");
                    canvas.SetAttribute("data-sparkle-density", (effects.Sparkle.Density ?? 0).ToString());
                    canvas.SetAttribute("data-sparkle-speed", (effects.Sparkle.Speed ?? 0).ToString());
                    wrapper.AppendChild(canvas);
                }

                if (wrapper.ParentElement != body)
                {
                    body.AppendChild(wrapper);
                }
            }

            private static void EnsurePageEffectsScript(IDocument document, ContentModel content)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("script[data-page-effects='true']") is not null)
                {
                    return;
                }

                var script = document.CreateElement("script");
                script.SetAttribute("data-page-effects", "true");
                script.TextContent = @"
(function(){
  var canvas = document.querySelector('.lp-page-effects canvas[data-sparkle]');
  if (!canvas) return;
  var ctx = canvas.getContext('2d');
  if (!ctx) return;

  function resize(){
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
  }
  resize();
  window.addEventListener('resize', resize);

  var intensity = canvas.getAttribute('data-sparkle-intensity') || 'low';
  var baseCount = intensity === 'high' ? 40 : intensity === 'mid' ? 24 : 14;
  var density = parseInt(canvas.getAttribute('data-sparkle-density') || '0', 10);
  var speed = parseInt(canvas.getAttribute('data-sparkle-speed') || '0', 10);
  var count = Math.max(6, baseCount + density);
  var color = canvas.getAttribute('data-sparkle-color') || '#ffffff';
  var particles = [];
  for (var i=0;i<count;i++){
    particles.push({
      x: Math.random()*canvas.width,
      y: Math.random()*canvas.height,
      r: Math.random()*1.2+0.6,
      a: Math.random()*0.6+0.2,
      s: (Math.random()*0.15+0.05) + (speed/200)
    });
  }

  var last = performance.now();
  function tick(now){
    var dt = now - last; last = now;
    if (dt > 60 && particles.length > 8) {
      particles = particles.slice(0, Math.max(8, Math.floor(particles.length*0.7)));
    }
    ctx.clearRect(0,0,canvas.width,canvas.height);
    ctx.fillStyle = color;
    particles.forEach(function(p){
      p.y -= p.s;
      if (p.y < -10) { p.y = canvas.height + 10; p.x = Math.random()*canvas.width; }
      ctx.globalAlpha = p.a;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r, 0, Math.PI*2);
      ctx.fill();
    });
    ctx.globalAlpha = 1;
    requestAnimationFrame(tick);
  }
  requestAnimationFrame(tick);
})();
";
                head.AppendChild(script);
            }

            private static bool IsFixedWhiteBackgroundSectionKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return false;
                }

                return key.Contains("conditions-contact-banners", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(key, "header", StringComparison.OrdinalIgnoreCase);
            }

            private static string BuildSectionBackgroundRule(SectionBackgroundSettings settings, string selector)
            {
                if (settings is null)
                {
                    return string.Empty;
                }

                var dto = BackgroundSettingMapper.FromSection(settings);
                return BackgroundStyleService.BuildRule(dto, selector);
            }

    private static void ApplyMeta(IDocument document, ContentModel content)
    {
        document.Title = content.Meta.PageTitle ?? string.Empty;
        var meta = document.QuerySelector("meta[name='description']");
        if (meta is not null)
        {
            meta.SetAttribute("content", content.Meta.Description ?? string.Empty);
        }
    }

    private static void EnsureViewportMeta(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        var viewport = document.QuerySelector("meta[name='viewport']");
        if (viewport is null)
        {
            viewport = document.CreateElement("meta");
            viewport.SetAttribute("name", "viewport");
            head.AppendChild(viewport);
        }

        viewport.SetAttribute("content", "width=device-width, initial-scale=1");
    }

    private static void EnsureBaseHref(IDocument document, string href)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        var baseTag = document.QuerySelector("base");
        if (baseTag is null)
        {
            baseTag = document.CreateElement("base");
            head.Prepend(baseTag);
        }

        baseTag.SetAttribute("href", href);
    }

    private static void EnsureEmphasisStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-emphasis='true']") is not null)
        {
            return;
        }

        var style = document.CreateElement("style");
        style.SetAttribute("data-emphasis", "true");
        style.TextContent = ".is-emphasis { font-weight: 700; color: #d32f2f; }";
        head.AppendChild(style);
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

    private static string? SanitizeFontFamily(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, "^[a-zA-Z0-9\\s,'\"-]+$") ? trimmed : null;
    }

    private static string? SanitizeTextAlign(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "left" => "left",
            "center" => "center",
            "right" => "right",
            "justify" => "justify",
            _ => null
        };
    }

    private static string ResolveBackgroundPosition(LpBackgroundModel background)
    {
        if (string.Equals(background.Position, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(background.PositionCustom) ? "center top" : background.PositionCustom;
        }

        return string.IsNullOrWhiteSpace(background.Position) ? "center top" : background.Position;
    }

    private static string ResolveBackgroundSize(LpBackgroundModel background)
    {
        if (string.Equals(background.Size, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(background.SizeCustom) ? "cover" : background.SizeCustom;
        }

        return string.IsNullOrWhiteSpace(background.Size) ? "cover" : background.Size;
    }

    private static string EscapeCssUrl(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static void SetCssCustomProperty(IElement element, string name, string value)
    {
        var current = element.GetAttribute("style") ?? string.Empty;
        var cleaned = Regex.Replace(current, $"{Regex.Escape(name)}\\s*:[^;]+;?", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (!string.IsNullOrWhiteSpace(cleaned) && !cleaned.EndsWith(";", StringComparison.Ordinal))
        {
            cleaned += ";";
        }

        element.SetAttribute("style", string.Concat(cleaned, $"{name}: {value};"));
    }

    private static void RemoveCssCustomProperty(IElement element, string name)
    {
        var current = element.GetAttribute("style") ?? string.Empty;
        var cleaned = Regex.Replace(current, $"{Regex.Escape(name)}\\s*:[^;]+;?", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            element.RemoveAttribute("style");
            return;
        }

        if (!cleaned.EndsWith(";", StringComparison.Ordinal))
        {
            cleaned += ";";
        }

        element.SetAttribute("style", cleaned);
    }

    private static IElement EnsureBackgroundWrapper(IDocument document, ContentModel content)
    {
        var body = document.Body;
        if (body is null)
        {
            return document.DocumentElement ?? document.CreateElement("div");
        }

        var canvas = document.QuerySelector(".lp-canvas") as IElement ?? document.CreateElement("div");
        canvas.ClassList.Add("lp-canvas");

        if (canvas.ParentElement != body)
        {
            body.Prepend(canvas);
        }

        var footer = document.QuerySelector("footer") as IElement
            ?? document.QuerySelector(".footer") as IElement;

        var moveTargets = body.Children
            .Where(child => !string.Equals(child.TagName, "SCRIPT", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(child.TagName, "STYLE", StringComparison.OrdinalIgnoreCase)
                            && child != canvas
                            && child != footer)
            .ToList();

        foreach (var child in moveTargets)
        {
            child.Remove();
            canvas.AppendChild(child);
        }

        if (footer is not null && footer.ParentElement != body)
        {
            footer.Remove();
            body.AppendChild(footer);
        }

        return canvas;
    }

    private static void MoveElements(IEnumerable<IElement> elements, IElement target, params IElement[] protectedElements)
    {
        foreach (var element in elements)
        {
            if (element is null || element == target)
            {
                continue;
            }

            if (protectedElements.Any(protectedElement => element == protectedElement || IsDescendantOf(element, protectedElement)))
            {
                continue;
            }

            if (IsDescendantOf(element, target))
            {
                continue;
            }

            element.Remove();
            target.AppendChild(element);
        }
    }

    private static bool IsDescendantOf(IElement element, IElement ancestor)
    {
        var current = element.ParentElement;
        while (current is not null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.ParentElement;
        }
        return false;
    }

    private static IEnumerable<IElement> FilterOuterMostElements(IEnumerable<IElement> elements)
    {
        var list = elements.Where(element => element is not null).Distinct().ToList();
        var set = new HashSet<IElement>(list);
        return list.Where(element => !GetAncestors(element).Any(ancestor => set.Contains(ancestor))).ToList();
    }

    private static IEnumerable<IElement> GetAncestors(IElement element)
    {
        var current = element.ParentElement;
        while (current is not null)
        {
            yield return current;
            current = current.ParentElement;
        }
    }

    private static IEnumerable<IElement> GetEditorManagedElements(IDocument document, ContentModel content)
    {
        var elements = new List<IElement>();

        var heroSelectors = new[] { ".mv", ".mv-wrap", ".mv__wrap", ".mv__wrapper", ".hero", ".hero-wrap", ".hero__wrap" };
        foreach (var selector in heroSelectors)
        {
            var hero = document.QuerySelector(selector) as IElement;
            if (hero is not null)
            {
                elements.Add(hero);
                break;
            }
        }

        foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
        {
            var key = group.GetAttribute("data-section");
            if (IsEditorManagedSectionKey(key, content))
            {
                elements.Add(group);
            }
        }

        return elements;
    }

    private static IEnumerable<IElement> GetFixedTopElements(IDocument document)
    {
        var selector = "header, .header, .header-logo, .header-logo__img, .l-header";
        return document.QuerySelectorAll(selector).ToList();
    }

    private static IEnumerable<IElement> GetFixedBottomElements(IDocument document)
    {
        var selector = ".section-group[data-section='conditions-contact-banners'], section.conditions, section.contact, .banner-head, a.banner, .magazine, footer, .footer, .footer-logo, .footer-logo__img";
        return document.QuerySelectorAll(selector).ToList();
    }


    private static void ApplySectionFonts(IDocument document, ContentModel content)
    {
        ApplySectionFont(document, "campaign-content", content.Sections.CampaignContent.FontFamily);
        ApplySectionFont(document, "campaignContent", content.Sections.CampaignContent.FontFamily);
        ApplySectionFont(document, "coupon-period", content.Sections.CouponPeriod.FontFamily);
        ApplySectionFont(document, "couponPeriod", content.Sections.CouponPeriod.FontFamily);
        ApplySectionFont(document, "store-search", content.Sections.StoreSearch.FontFamily);
        ApplySectionFont(document, "storeSearch", content.Sections.StoreSearch.FontFamily);
        ApplySectionFont(document, "coupon-notes", content.Sections.CouponNotes.FontFamily);
        ApplySectionFont(document, "couponNotes", content.Sections.CouponNotes.FontFamily);
        ApplySectionFont(document, "ranking", content.Sections.Ranking.FontFamily);
        ApplySectionFont(document, "conditions", content.Sections.Conditions.FontFamily);
        ApplySectionFont(document, "contact", content.Sections.Contact.FontFamily);

        if (!string.IsNullOrWhiteSpace(content.Sections.Banners.FontFamily))
        {
            foreach (var element in document.QuerySelectorAll(".banner-head, a.banner, .magazine").ToList())
            {
                ApplyFontStyle(element, content.Sections.Banners.FontFamily);
            }
        }

        if (!string.IsNullOrWhiteSpace(content.Campaign.FooterFontFamily))
        {
            foreach (var element in document.QuerySelectorAll(".section-group[data-section='countdown']").ToList())
            {
                ApplyFontStyle(element, content.Campaign.FooterFontFamily);
            }
        }

        if (content.CustomSections is not null)
        {
            foreach (var section in content.CustomSections)
            {
                if (string.IsNullOrWhiteSpace(section.Key) || string.IsNullOrWhiteSpace(section.FontFamily))
                {
                    continue;
                }

                foreach (var element in document.QuerySelectorAll($".section-group[data-section='{section.Key}']").ToList())
                {
                    ApplyFontStyle(element, section.FontFamily);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(content.Sections.StoreSearch.FontFamily))
        {
            foreach (var element in document.QuerySelectorAll(".section-group[data-section^='store-search']").ToList())
            {
                ApplyFontStyle(element, content.Sections.StoreSearch.FontFamily);
            }
        }
    }

    private static void ApplySectionTextAligns(IDocument document, ContentModel content)
    {
        ApplySectionTextAlign(document, "campaign-content", content.Sections.CampaignContent.TextAlign);
        ApplySectionTextAlign(document, "campaignContent", content.Sections.CampaignContent.TextAlign);
        ApplySectionTextAlign(document, "coupon-period", content.Sections.CouponPeriod.TextAlign);
        ApplySectionTextAlign(document, "couponPeriod", content.Sections.CouponPeriod.TextAlign);
        ApplySectionTextAlign(document, "store-search", content.Sections.StoreSearch.TextAlign);
        ApplySectionTextAlign(document, "storeSearch", content.Sections.StoreSearch.TextAlign);
        ApplySectionTextAlignSelector(document, ".section-group[data-section^='store-search']", content.Sections.StoreSearch.TextAlign);
        ApplySectionTextAlign(document, "coupon-notes", content.Sections.CouponNotes.TextAlign);
        ApplySectionTextAlign(document, "couponNotes", content.Sections.CouponNotes.TextAlign);
        ApplySectionTextAlign(document, "ranking", content.Sections.Ranking.TextAlign);
        ApplySectionTextAlignSelector(document, "section.conditions", content.Sections.Conditions.TextAlign);
        ApplySectionTextAlignSelector(document, "section.contact", content.Sections.Contact.TextAlign);
        ApplySectionTextAlignSelector(document, ".banner-head, a.banner, .magazine", content.Sections.Banners.TextAlign);

        if (content.CustomSections is not null)
        {
            foreach (var section in content.CustomSections)
            {
                if (string.IsNullOrWhiteSpace(section.Key))
                {
                    continue;
                }

                ApplySectionTextAlign(document, section.Key, section.TextAlign);
            }
        }
    }

    private static void ApplySectionTextAlign(IDocument document, string key, string? textAlign)
    {
        ApplySectionTextAlignSelector(document, $".section-group[data-section='{key}']", textAlign);
    }

    private static void ApplySectionTextAlignSelector(IDocument document, string selector, string? textAlign)
    {
        var safe = SanitizeTextAlign(textAlign);
        if (string.IsNullOrWhiteSpace(safe))
        {
            return;
        }

        foreach (var element in document.QuerySelectorAll(selector).ToList())
        {
            ApplyTextAlignStyle(element, safe);
        }
    }

    private static void ApplyTextAlignStyle(IElement element, string textAlign)
    {
        var current = element.GetAttribute("style") ?? string.Empty;
        if (current.Contains("text-align", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var addition = $"text-align: {textAlign};";
        element.SetAttribute("style", string.Concat(current, current.EndsWith(";") || current.Length == 0 ? string.Empty : "; ", addition));
    }

    private static void ApplySectionFont(IDocument document, string key, string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return;
        }

        foreach (var element in document.QuerySelectorAll($".section-group[data-section='{key}']").ToList())
        {
            ApplyFontStyle(element, fontFamily);
        }
    }

    private static void ApplyFontStyle(IElement element, string? fontFamily)
    {
        var safe = SanitizeFontFamily(fontFamily);
        if (string.IsNullOrWhiteSpace(safe))
        {
            return;
        }

        var current = element.GetAttribute("style") ?? string.Empty;
        var addition = $"font-family: {safe};";
        if (!current.Contains("font-family", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttribute("style", string.Concat(current, current.EndsWith(";") || current.Length == 0 ? string.Empty : "; ", addition));
        }
    }

    private static void ApplyCampaignStyle(IDocument document, ContentModel content)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        var boxColor = SanitizeCssColor(content.CampaignStyle.BoxColor);
        var headingColor = SanitizeCssColor(content.CampaignStyle.HeadingColor);
        var headingBackgroundColor = SanitizeCssColor(content.CampaignStyle.HeadingBackgroundColor);
        var frameBorderColor = SanitizeCssColor(content.CampaignStyle.FrameBorderColor);
        var textColor = SanitizeCssColor(content.CampaignStyle.TextColor);

        var mvFooterColor = SanitizeCssColor(content.CampaignStyle.MvFooterBackgroundColor);
        var backgroundRules = BuildBackgroundPresetRules(content);

        if (boxColor is null && headingColor is null && headingBackgroundColor is null && frameBorderColor is null && textColor is null && mvFooterColor is null && string.IsNullOrWhiteSpace(backgroundRules))
        {
            return;
        }

        var rules = new List<string>();
        if (boxColor is not null)
        {
            rules.Add($".campaign__box, .campaign__subBox {{ background-color: {boxColor} !important; }}");
        }

        if (headingColor is not null)
        {
            rules.Add($".campaign__heading, .campaign__heading * {{ color: {headingColor} !important; }}");
        }

        if (headingBackgroundColor is not null)
        {
            rules.Add($".campaign__heading {{ background-color: {headingBackgroundColor} !important; }}");
        }

        if (frameBorderColor is not null)
        {
            rules.Add($".campaign__block, .campaign__box {{ border-color: {frameBorderColor} !important; }}");
        }

        if (textColor is not null)
        {
            rules.Add($".campaign__text, .campaign__subBox, .campaign__subBox li {{ color: {textColor} !important; }}");
        }

        if (mvFooterColor is not null)
        {
            rules.Add($".mv-footer {{ background-color: {mvFooterColor} !important; }}");
        }

        if (!string.IsNullOrWhiteSpace(backgroundRules))
        {
            rules.Add(backgroundRules);
        }

        var style = document.CreateElement("style");
        style.SetAttribute("data-campaign-style", "true");
        style.TextContent = string.Join(Environment.NewLine, rules);
        head.AppendChild(style);
    }

    private static string BuildBackgroundPresetRules(ContentModel content)
    {
        var preset = content.CampaignStyle.BackgroundPreset ?? string.Empty;
        if (string.IsNullOrWhiteSpace(preset))
        {
            return string.Empty;
        }

        var colorA = SanitizeCssColor(content.CampaignStyle.BackgroundColorA) ?? "#f5f5ff";
        var colorB = SanitizeCssColor(content.CampaignStyle.BackgroundColorB) ?? "#ffffff";

        var selector = "body, .page, .lp-wrapper, .l-wrapper";
        return preset switch
        {
            "gradient-royal" => $"{selector} {{ background: linear-gradient(135deg, {colorA}, {colorB}) !important; }}",
            "gradient-sunset" => $"{selector} {{ background: linear-gradient(135deg, {colorA}, {colorB}) !important; }}",
            "dots" => $"{selector} {{ background-color: {colorA} !important; background-image: radial-gradient({colorB} 1px, transparent 1px) !important; background-size: 18px 18px !important; }}",
            "stripes" => $"{selector} {{ background-color: {colorA} !important; background-image: repeating-linear-gradient(45deg, {colorB} 0, {colorB} 6px, transparent 6px, transparent 12px) !important; }}",
            "grid" => $"{selector} {{ background-color: {colorA} !important; background-image: linear-gradient(0deg, {colorB} 1px, transparent 1px), linear-gradient(90deg, {colorB} 1px, transparent 1px) !important; background-size: 24px 24px !important; }}",
            _ => string.Empty
        };
    }

    private static void ApplyHeader(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var logoImages = document.QuerySelectorAll(".header-logo__img img, .footer-logo__img img");
        foreach (var img in logoImages)
        {
            if (IsImageDeleted(content, content.Header.LogoImage))
            {
                img.Remove();
                continue;
            }

            img.SetAttribute("alt", content.Header.LogoAlt ?? string.Empty);
            img.SetAttribute("src", ResolveImageUrl(content.Header.LogoImage, template, overrides, embedImages));
        }
    }

    private static void ApplyHero(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var heroPicture = document.QuerySelector(".mv picture");
        var heroPc = heroPicture?.QuerySelector("img") ?? document.QuerySelector(".mv img");
        var heroSp = heroPicture?.QuerySelector("source[media]");

        if (heroPc is not null)
        {
            if (IsImageDeleted(content, content.Hero.ImagePc))
            {
                heroPc.Remove();
            }
            else
            {
                heroPc.SetAttribute("src", ResolveImageUrl(content.Hero.ImagePc, template, overrides, embedImages));
                heroPc.SetAttribute("alt", content.Hero.Alt ?? string.Empty);
            }
        }

        if (heroSp is not null)
        {
            if (IsImageDeleted(content, content.Hero.ImageSp))
            {
                heroSp.Remove();
            }
            else
            {
                heroSp.SetAttribute("srcset", ResolveImageUrl(content.Hero.ImageSp, template, overrides, embedImages));
            }
        }
        else
        {
            var spCandidates = document.QuerySelectorAll(
                ".mv .sp img, .mv img.sp, .mv img.sp-only, .mv img.is-sp, .mv img.mobile, .mv .mv-sp img, .mv .mv__sp img").ToList();

            if (spCandidates.Count > 0)
            {
                foreach (var img in spCandidates)
                {
                    if (IsImageDeleted(content, content.Hero.ImageSp))
                    {
                        img.Remove();
                        continue;
                    }

                    img.SetAttribute("src", ResolveImageUrl(content.Hero.ImageSp, template, overrides, embedImages));
                    img.SetAttribute("alt", content.Hero.Alt ?? string.Empty);
                }
            }
            else if (heroPicture is not null && heroPc is not null && !IsImageDeleted(content, content.Hero.ImageSp))
            {
                var source = document.CreateElement("source");
                source.SetAttribute("media", "(max-width: 768px)");
                source.SetAttribute("srcset", ResolveImageUrl(content.Hero.ImageSp, template, overrides, embedImages));
                heroPicture.InsertBefore(source, heroPc);
            }
        }
    }

    private static void ApplySections(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        EnsureCustomSections(document, content, template, overrides, embedImages);
        EnsureRankingSection(document, content, template, overrides, embedImages);
        EnsureStoreSearchSection(document, content, template, overrides, embedImages);
        ApplySectionGroups(document, template, content);

        var campaignContentSection = document.QuerySelector(".section-group[data-section='campaign-content']")
            ?? document.QuerySelector(".section-group[data-section='campaignContent']");
        var couponNotesSection = document.QuerySelector(".section-group[data-section='coupon-notes']")
            ?? document.QuerySelector(".section-group[data-section='couponNotes']");
        var couponPeriodSection = document.QuerySelector(".section-group[data-section='coupon-period']")
            ?? document.QuerySelector(".section-group[data-section='couponPeriod']");
        var storeSearchSection = document.QuerySelector(".section-group[data-section='store-search']")
            ?? document.QuerySelector(".section-group[data-section='storeSearch']");
        var rankingSection = document.QuerySelector(".section-group[data-section='ranking']");
        var countdownSection = document.QuerySelector(".section-group[data-section='countdown']");

        if (campaignContentSection is not null
            && !content.Sections.CampaignContent.Enabled)
        {
            campaignContentSection.Remove();
            campaignContentSection = null;
        }

        if (couponNotesSection is not null
            && !content.Sections.CouponNotes.Enabled)
        {
            couponNotesSection.Remove();
            couponNotesSection = null;
        }

        if (couponPeriodSection is not null
            && !content.Sections.CouponPeriod.Enabled)
        {
            couponPeriodSection.Remove();
            couponPeriodSection = null;
        }

        if (storeSearchSection is not null
            && !content.Sections.StoreSearch.Enabled)
        {
            storeSearchSection.Remove();
            storeSearchSection = null;
        }

        if (rankingSection is not null
            && !content.Sections.Ranking.Enabled)
        {
            rankingSection.Remove();
            rankingSection = null;
        }

        var conditionsSection = document.QuerySelector("section.conditions");
        var contactSection = document.QuerySelector("section.contact");
        var allowFixedSectionEdits = false;

        if (allowFixedSectionEdits)
        {
            if (conditionsSection is not null
                && !content.Sections.Conditions.Enabled)
            {
                conditionsSection.Remove();
                conditionsSection = null;
            }

            if (contactSection is not null
                && !content.Sections.Contact.Enabled)
            {
                contactSection.Remove();
                contactSection = null;
            }

            if (!content.Sections.Banners.Enabled)
            {
                document.QuerySelector(".banner-head")?.Remove();
                document.QuerySelector("a.banner")?.Remove();
                document.QuerySelector(".magazine")?.Remove();
            }
        }

        var dateRangeText = FormatDateRange(content.Campaign.StartDate, content.Campaign.EndDate);
        ApplyCountdownDisplay(document, content, dateRangeText);

        if (campaignContentSection is not null && content.Sections.CampaignContent.Enabled)
        {
            var blocks = campaignContentSection.QuerySelectorAll(".campaign__block").ToList();
            var mainBlock = blocks.Count > 0 ? blocks[0] : null;
            var dateBlock = blocks.Count > 1 ? blocks[1] : null;

            if (mainBlock is not null)
            {
                SetText(mainBlock, content.Sections.CampaignContent.Title, ".campaign__heading");
                SetHtmlText(mainBlock, content.Sections.CampaignContent.Body, ".campaign__text");
                SetList(mainBlock, content.Sections.CampaignContent.Notes, ".campaign__subBox .c-list");
            }

            if (dateBlock is not null && !string.IsNullOrWhiteSpace(dateRangeText))
            {
                SetText(dateBlock, dateRangeText, ".campaign__text");
            }
        }

        if (couponNotesSection is not null && content.Sections.CouponNotes.Enabled)
        {
            SetText(couponNotesSection, content.Sections.CouponNotes.Title, ".campaign__heading");
            // CouponNotes.Items(TextItemModel) -> CouponNotes.TextLines(StyledTextItem)
            var couponNoteLines = content.Sections.CouponNotes.TextLines.Count > 0
                ? content.Sections.CouponNotes.TextLines
                : MapTextItemsToStyledLines(content.Sections.CouponNotes.Items, content.Sections.CouponNotes.FontFamily, content.Sections.CouponNotes.TextAlign);
            SetStyledList(couponNotesSection, couponNoteLines, ".c-list");
        }

        if (couponPeriodSection is not null && content.Sections.CouponPeriod.Enabled)
        {
            SetText(couponPeriodSection, content.Sections.CouponPeriod.Title, ".campaign__heading");
            var periodText = ResolveCouponPeriodText(content);
            SetHtmlText(couponPeriodSection, periodText, ".campaign__text");
        }

        if (storeSearchSection is not null && content.Sections.StoreSearch.Enabled)
        {
            SetText(storeSearchSection, content.Sections.StoreSearch.Title, ".campaign__heading");
            SetText(storeSearchSection, content.Sections.StoreSearch.NoticeTitle, ".store-search-notice-heading");
            // StoreSearch.NoticeItems(TextItemModel) -> StoreSearch.NoticeLines(StyledTextItem)
            var noticeLines = content.Sections.StoreSearch.NoticeLines.Count > 0
                ? content.Sections.StoreSearch.NoticeLines
                : MapTextItemsToStyledLines(content.Sections.StoreSearch.NoticeItems, content.Sections.StoreSearch.FontFamily, content.Sections.StoreSearch.TextAlign);
            SetStyledList(storeSearchSection, noticeLines, ".store-search-notice-list");
        }

        if (countdownSection is not null)
        {
            // Campaign.EndedMessage(旧単行) -> Campaign.FooterLines(StyledTextItem)
            UpsertFooterTextLines(document, countdownSection, content.Campaign.FooterLines);
        }

        if (content.Sections.Ranking.Enabled)
        {
            if (rankingSection is not null)
            {
                UpdateRankingSection(rankingSection, content, template, overrides, embedImages);
            }
        }

        UpdateCustomSections(document, content, template, overrides, embedImages);

        PlaceSectionsAfterMvFooter(content, document, campaignContentSection, couponNotesSection, conditionsSection, contactSection);

        if (allowFixedSectionEdits)
        {
            if (conditionsSection is not null && content.Sections.Conditions.Enabled)
            {
                var titleImage = conditionsSection.QuerySelector(".conditions__title img");
                if (titleImage is not null)
                {
                    if (IsImageDeleted(content, content.Sections.Conditions.TitleImage))
                    {
                        titleImage.Remove();
                    }
                    else
                    {
                        titleImage.SetAttribute("src", ResolveImageUrl(content.Sections.Conditions.TitleImage, template, overrides, embedImages));
                    }
                }

                var textImage = conditionsSection.QuerySelector(".conditions__text img");
                if (textImage is not null)
                {
                    if (IsImageDeleted(content, content.Sections.Conditions.TextImage))
                    {
                        textImage.Remove();
                    }
                    else
                    {
                        textImage.SetAttribute("src", ResolveImageUrl(content.Sections.Conditions.TextImage, template, overrides, embedImages));
                    }
                }

                SetText(conditionsSection, content.Sections.Conditions.DeviceText, ".conditions__model");
                SetList(conditionsSection, content.Sections.Conditions.Items, ".conditionsNotesBox__notes");
            }

            if (contactSection is not null && content.Sections.Contact.Enabled)
            {
                SetHtmlText(contactSection, content.Sections.Contact.Title, ".contact__title");
                SetHtmlText(contactSection, content.Sections.Contact.Lead, ".contact__lead");
                SetContactButtons(contactSection, content.Sections.Contact.Buttons, content.Sections.Contact.OfficeHours);
            }

            if (content.Sections.Banners.Enabled)
            {
                var mainBanner = document.QuerySelector("a.banner");
                if (mainBanner is not null)
                {
                    mainBanner.SetAttribute("href", content.Sections.Banners.Main.Url ?? string.Empty);
                    var img = mainBanner.QuerySelector(".banner__main img");
                    if (img is not null)
                    {
                        if (IsImageDeleted(content, content.Sections.Banners.Main.Image))
                        {
                            img.Remove();
                        }
                        else
                        {
                            img.SetAttribute("src", ResolveImageUrl(content.Sections.Banners.Main.Image, template, overrides, embedImages));
                        }
                    }
                }

                var magazineBanner = document.QuerySelector(".magazine__btn");
                if (magazineBanner is not null)
                {
                    magazineBanner.SetAttribute("href", content.Sections.Banners.Magazine.Url ?? string.Empty);
                    var img = magazineBanner.QuerySelector("img");
                    if (img is not null)
                    {
                        if (IsImageDeleted(content, content.Sections.Banners.Magazine.Image))
                        {
                            img.Remove();
                        }
                        else
                        {
                            img.SetAttribute("src", ResolveImageUrl(content.Sections.Banners.Magazine.Image, template, overrides, embedImages));
                        }
                    }
                }
            }
        }
    }

        private static void EnsureCustomSections(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (content.CustomSections is null || content.CustomSections.Count == 0)
            {
                return;
            }

            var parent = GetSectionGroupParent(document);
            if (parent is null)
            {
                return;
            }

            foreach (var section in content.CustomSections)
            {
                if (string.IsNullOrWhiteSpace(section.Key))
                {
                    continue;
                }

                var selector = $".section-group[data-section='{section.Key}']";
                var existing = document.QuerySelector(selector);
                if (existing is null)
                {
                    var element = CreateCustomSection(document, section, content, template, overrides, embedImages);
                    parent.AppendChild(element);
                }
                else
                {
                    UpdateCustomSection(existing, section, content, template, overrides, embedImages);
                }
            }
        }

        private static void UpdateCustomSections(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (content.CustomSections is null || content.CustomSections.Count == 0)
            {
                foreach (var element in document.QuerySelectorAll(".section-group.custom-section[data-section]").ToList())
                {
                    element.Remove();
                }
                return;
            }

            var keys = new HashSet<string>(
                content.CustomSections.Where(section => !string.IsNullOrWhiteSpace(section.Key)).Select(section => section.Key),
                StringComparer.OrdinalIgnoreCase);

            foreach (var element in document.QuerySelectorAll(".section-group.custom-section[data-section]").ToList())
            {
                var key = element.GetAttribute("data-section")?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !keys.Contains(key))
                {
                    element.Remove();
                }
            }

            foreach (var section in content.CustomSections)
            {
                if (string.IsNullOrWhiteSpace(section.Key))
                {
                    continue;
                }

                var element = document.QuerySelector($".section-group[data-section='{section.Key}']");
                if (element is null)
                {
                    continue;
                }

                UpdateCustomSection(element, section, content, template, overrides, embedImages);
            }
        }

        private static void EnsureRankingSection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (!content.Sections.Ranking.Enabled)
            {
                return;
            }

            var rankingSection = document.QuerySelector(".section-group[data-section='ranking']");
            if (rankingSection is null)
            {
                var parent = GetSectionGroupParent(document);
                if (parent is null)
                {
                    return;
                }

                rankingSection = CreateRankingSection(document, content, template, overrides, embedImages);
                parent.AppendChild(rankingSection);
            }
            else
            {
                UpdateRankingSection(rankingSection, content, template, overrides, embedImages);
            }
        }

        private static void EnsureStoreSearchSection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (!content.Sections.StoreSearch.Enabled)
            {
                return;
            }

            var keys = content.SectionGroups
                .Where(group => !string.IsNullOrWhiteSpace(group.Key)
                    && (group.Key.StartsWith("store-search", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(group.Key, "storeSearch", StringComparison.OrdinalIgnoreCase)))
                .Select(group => group.Key)
                .ToList();

            if (keys.Count == 0)
            {
                return;
            }

            var parent = GetSectionGroupParent(document);
            if (parent is null)
            {
                return;
            }

            foreach (var key in keys)
            {
                var selector = $".section-group[data-section='{key}']";
                var section = document.QuerySelector(selector);
                if (section is null)
                {
                    var wrapper = CreateStoreSearchSection(document, content, template, overrides, embedImages, key);
                    parent.AppendChild(wrapper);
                }
                else
                {
                    section.ClassList.Add("store-search-section");
                    section.InnerHtml = BuildStoreSearchSectionHtml(content, template, overrides, embedImages);
                }
            }
        }

        private static IElement? GetSectionGroupParent(IDocument document)
        {
            var firstGroup = document.QuerySelector(".section-group");
            if (firstGroup?.ParentElement is not null)
            {
                return firstGroup.ParentElement;
            }

            return document.Body ?? document.DocumentElement;
        }

        private static void EnsureStoreSearchStyle(IDocument document, ContentModel content)
        {
            if (!content.Sections.StoreSearch.Enabled)
            {
                return;
            }

            var head = document.Head;
            if (head is null || document.QuerySelector("style[data-store-search-style='true']") is not null)
            {
                return;
            }

            var style = document.CreateElement("style");
            style.SetAttribute("data-store-search-style", "true");
            style.TextContent = @"
    .store-search-section .store-search-body { padding: 18px 14px; }
    .store-search-section .store-search-notice { background: #fff6c7; border: 1px solid #f5d68b; padding: 12px; border-radius: 10px; }
    .store-search-section .store-search-notice-heading { color: #c81d1d; font-weight: 700; margin: 0 0 8px; }
    .store-search-section .store-search-notice-list { margin: 0; padding-left: 18px; }
    .store-search-section .store-search-search { margin: 16px 0 12px; }
    .store-search-section .store-search-filters { display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; font-weight: 700; }
    .store-search-section .store-search-filters label { display: inline-flex; align-items: center; gap: 6px; }
    .store-search-section .store-search-input { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; }
    .store-search-section .store-search-input input { flex: 1; min-width: 200px; padding: 10px 12px; border-radius: 6px; border: 1px solid #d0d0d0; }
    .store-search-section .store-search-input button { background: #e65a2a; color: #fff; border: none; border-radius: 6px; padding: 10px 14px; font-weight: 700; }
    .store-search-section .store-search-result { margin: 10px 0; font-weight: 700; color: #c81d1d; }
    .store-search-section .store-search-pager { display: flex; align-items: center; gap: 8px; margin: 10px 0; }
    .store-search-section .store-search-pager button { background: #ffffff; color: #e65a2a; border: 1px solid #f0b7a0; border-radius: 6px; padding: 6px 12px; font-weight: 700; }
    .store-search-section .store-search-pager button:disabled { opacity: 0.5; cursor: not-allowed; }
    .store-search-section .store-search-page-info { font-weight: 700; color: #1f2937; }
    .store-search-section .store-search-list { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .store-search-section .store-search-card { border: 1px solid #f0b7a0; border-radius: 8px; padding: 10px; background: #fff; }
    .store-search-section .store-search-card-title { font-weight: 700; }
    .store-search-section .store-search-map-link { color: #1d4ed8; text-decoration: underline; }
    .store-search-section .store-search-card-address { font-size: 0.85rem; color: #6b7280; margin-top: 4px; }
    .store-search-section .store-search-distance { margin-top: 6px; font-size: 0.8rem; font-weight: 700; color: #e65a2a; }
    .store-search-section .store-labels { display: flex; gap: 6px; margin: 6px 0; flex-wrap: wrap; }
    .store-search-section .store-label { display: inline-flex; align-items: center; padding: 2px 8px; border-radius: 999px; font-size: 0.72rem; font-weight: 700; }
    .store-search-section .store-label.raffle { background: #ffe4e6; color: #be123c; border: 1px solid #fda4af; }
    .store-search-section .store-label.coupon { background: #dbeafe; color: #1d4ed8; border: 1px solid #93c5fd; }
    .store-search-section .store-label.campaign { background: #dcfce7; color: #15803d; border: 1px solid #86efac; }
    ";
            head.AppendChild(style);
        }

    private static void EnsureRankingStyle(IDocument document, ContentModel content)
    {
        if (!content.Sections.Ranking.Enabled)
        {
            return;
        }

        var head = document.Head;
        if (head is null || document.QuerySelector("style[data-ranking-style='true']") is not null)
        {
            return;
        }

        var ranking = content.Sections.Ranking;
        var colors = ResolveRankingTableColors(ranking);
        var borderWidth = (ranking.TableBorderWidth is > 0) ? ranking.TableBorderWidth.Value : 4;
        var tableFont = SanitizeFontFamily(ranking.TableFontFamily);
        var tableTextColor = SanitizeCssColor(ranking.TableTextColor);
        var tableTextStrokeColor = SanitizeCssColor(ranking.TableTextStrokeColor);

        var rules = new List<string>
        {
            $".ranking-section .ranking-table {{ width: 100%; border-collapse: collapse; margin-top: 8px; }}",
            $".ranking-section .ranking-table th, .ranking-section .ranking-table td {{ border: {borderWidth}px solid {colors.Border}; padding: 8px 10px; text-align: center; }}",
            $".ranking-section .ranking-table th {{ background: {colors.HeaderBg}; color: {colors.HeaderText}; font-weight: 700; }}",
            $".ranking-section .ranking-table tbody tr:nth-child(odd) {{ background: {colors.Stripe}; }}",
            ".ranking-section .ranking-meta { display: flex; gap: 10px; flex-wrap: wrap; font-size: 0.9rem; color: #4b5563; }",
            ".ranking-section .ranking-period-label { font-weight: 700; margin-right: 4px; }",
            ".ranking-section .ranking-free-texts { margin-top: 8px; display: grid; gap: 6px; }",
            ".ranking-section .ranking-table-notes { margin-top: 10px; display: grid; gap: 6px; font-size: 0.85rem; color: #6b7280; }",
            ".ranking-section .ranking-text-line { width: 100%; }",
            ".ranking-section .ranking-crown { position: relative; display: inline-flex; align-items: center; justify-content: center; width: 48px; height: 40px; margin-right: 6px; font-weight: 700; line-height: 1; }",
            ".ranking-section .ranking-crown .crown-icon { width: 38px; height: 38px; line-height: 1; display: block; }",
            ".ranking-section .ranking-crown .crown-rank { position: absolute; top: 12px; left: 50%; transform: translateX(-50%); font-size: 16px; color: #111827; }",
            ".ranking-section .ranking-crown .crown-unit { position: absolute; top: 26px; left: 50%; transform: translateX(-50%); font-size: 10px; color: #111827; }",
            ".ranking-section .ranking-crown.rank-1 .crown-icon { color: #fbbf24; }",
            ".ranking-section .ranking-crown.rank-2 .crown-icon { color: #9ca3af; }",
            ".ranking-section .ranking-crown.rank-3 .crown-icon { color: #d97706; }",
            "@media (max-width: 767px) { .section-group.ranking-section .campaign__box, .section-group.ranking-section .ranking-table { width: 100% !important; } }"
        };

        if (!string.IsNullOrWhiteSpace(tableFont))
        {
            rules.Add($".ranking-section .ranking-table {{ font-family: {tableFont}; }}");
        }

        if (!string.IsNullOrWhiteSpace(tableTextColor))
        {
            rules.Add($".ranking-section .ranking-table td {{ color: {tableTextColor}; }}");
        }

        if (ranking.TableTextBold)
        {
            rules.Add(".ranking-section .ranking-table td { font-weight: 700; }");
        }

        if (!string.IsNullOrWhiteSpace(tableTextStrokeColor))
        {
            rules.Add($".ranking-section .ranking-table td {{ text-shadow: -0.6px 0 {tableTextStrokeColor}, 0 0.6px {tableTextStrokeColor}, 0.6px 0 {tableTextStrokeColor}, 0 -0.6px {tableTextStrokeColor}; }}");
        }

                rules.Add(@"
table.campaign_rank-box {
    width: var(--ranking-table-width, 100%);
    margin: 0 auto;
    border-collapse: collapse;
    table-layout: fixed;
}
table.campaign_rank-box th,
table.campaign_rank-box td {
    text-align: center;
    vertical-align: middle;
    padding: 18px 10px;
    font-weight: 800;
    line-height: 1.15;
    white-space: nowrap;
    border: 6px solid #b30000;
}
table.campaign_rank-box th {
    background: #fff3b0;
    font-size: 20px;
}
table.campaign_rank-box td {
    font-size: 26px;
}
table.campaign_rank-box td.king {
    position: relative !important;
    height: 120px !important;
    min-height: 120px !important;
    padding: 0 !important;
    vertical-align: middle !important;
    z-index: 1;
}
table.campaign_rank-box td.king::after {
    content: "" !important;
    position: absolute !important;
    left: 50% !important;
    top: 50% !important;
    transform: translate(-50%, -50%) !important;
    width: 120px !important;
    height: 90px !important;
    background-repeat: no-repeat !important;
    background-position: center !important;
    background-size: 120px 90px !important;
    pointer-events: none !important;
    z-index: 0;
}
table.campaign_rank-box td.king.king-01--number::after { background-image: url('images/icon-king-01.svg'); }
table.campaign_rank-box td.king.king-02--number::after { background-image: url('images/icon-king-02.svg'); }
table.campaign_rank-box td.king.king-03--number::after { background-image: url('images/icon-king-03.svg'); }
table.campaign_rank-box td.king .king-rank-text {
    position: relative;
    z-index: 1;
}
@media (max-width: 767px) {
    table.campaign_rank-box th,
    table.campaign_rank-box td {
        padding: 12px 6px;
        border-width: 4px;
    }
    table.campaign_rank-box th { font-size: 16px; }
    table.campaign_rank-box td { font-size: 20px; }
    table.campaign_rank-box td.king { min-height: 96px !important; height: 96px !important; }
    table.campaign_rank-box td.king::after { width: 96px !important; height: 72px !important; background-size: 96px 72px !important; }
}
");

        var style = document.CreateElement("style");
        style.SetAttribute("data-ranking-style", "true");
        style.TextContent = string.Join(Environment.NewLine, rules);
        head.AppendChild(style);
	}

    private static (string HeaderBg, string HeaderText, string Border, string Stripe) ResolveRankingTableColors(RankingSectionModel ranking)
    {
        var preset = ranking.TablePreset ?? string.Empty;
        return preset switch
        {
            "classic" => ("#0e0d6a", "#ffffff", "#0e0d6a", "#f3f4ff"),
            "gold" => ("#f59e0b", "#1f2937", "#f59e0b", "#fff7ed"),
            "mono" => ("#111827", "#ffffff", "#111827", "#f9fafb"),
            _ => (
                SanitizeCssColor(ranking.TableHeaderColor) ?? "#fff2b0",
                SanitizeCssColor(ranking.TableHeaderTextColor) ?? "#111827",
                SanitizeCssColor(ranking.TableBorderColor) ?? "#b91c1c",
                SanitizeCssColor(ranking.TableStripeColor) ?? "#ffffff"
            )
        };
    }

        private static void EnsureStoreSearchScript(IDocument document, ContentModel content)
        {
                if (!content.Sections.StoreSearch.Enabled)
                {
                        return;
                }

                var head = document.Head;
                if (head is null || document.QuerySelector("script[data-store-search-script='true']") is not null)
                {
                        return;
                }

                var script = document.CreateElement("script");
                script.SetAttribute("data-store-search-script", "true");
                script.TextContent = @"
            (function(){
    function toNumber(value){
        var n = parseFloat(value);
        return Number.isFinite(n) ? n : null;
    }
    function haversine(lat1, lon1, lat2, lon2){
        var R = 6371;
        var dLat = (lat2-lat1) * Math.PI/180;
        var dLon = (lon2-lon1) * Math.PI/180;
        var a = Math.sin(dLat/2) * Math.sin(dLat/2) + Math.cos(lat1*Math.PI/180) * Math.cos(lat2*Math.PI/180) * Math.sin(dLon/2) * Math.sin(dLon/2);
        var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
        return R * c;
    }

    function init(section){
        var list = section.querySelector('.store-search-list');
        if (!list) return;

        var cards = Array.prototype.slice.call(list.querySelectorAll('.store-search-card'));
        var input = section.querySelector('.store-search-keyword');
        var geo = section.querySelector('.store-search-geo');
        var result = section.querySelector('.store-search-result');
        var prev = section.querySelector('.store-search-prev');
        var next = section.querySelector('.store-search-next');
        var pageInfo = section.querySelector('.store-search-page-info');
        var filters = Array.prototype.slice.call(section.querySelectorAll('input[data-filter-key]'));
        var currentPage = 1;

        function isChecked(el){ return !!(el && el.checked); }

        function matches(card){
            var keyword = (input && input.value || '').trim().toLowerCase();
            var text = [card.dataset.id, card.dataset.name, card.dataset.zip, card.dataset.address].join(' ').toLowerCase();
            if (keyword && text.indexOf(keyword) === -1) return false;
            var active = filters.filter(function(el){ return isChecked(el); }).map(function(el){ return el.getAttribute('data-filter-key'); });
            if (active.length > 0) {
                var targets = (card.dataset.targets || '').split('|').filter(Boolean);
                var hit = active.some(function(key){ return targets.indexOf(key) !== -1; });
                if (!hit) return false;
            }
            return true;
        }

        function getPerPage(){
            return window.innerWidth <= 768 ? 6 : 10;
        }

        function apply(){
            var filtered = cards.filter(matches);
            var total = filtered.length;
            var perPage = getPerPage();
            var totalPages = Math.max(1, Math.ceil(total / perPage));
            if (currentPage > totalPages) currentPage = totalPages;

            filtered.forEach(function(card, index){
                var start = (currentPage - 1) * perPage;
                var end = start + perPage;
                card.style.display = (index >= start && index < end) ? '' : 'none';
            });

            cards.filter(function(card){ return filtered.indexOf(card) === -1; })
                .forEach(function(card){ card.style.display = 'none'; });

            if (result) result.textContent = '該当件数：' + total + '件';
            if (prev) prev.disabled = currentPage <= 1;
            if (next) next.disabled = currentPage >= totalPages;
        }

        function applyDistance(lat, lng){
            cards.forEach(function(card){
                var cLat = toNumber(card.dataset.lat);
                var cLng = toNumber(card.dataset.lng);
                var label = card.querySelector('.store-search-distance');
                if (cLat === null || cLng === null || !label) {
                    if (label) label.textContent = '距離: -';
                    card.dataset.distance = '';
                    return;
                }
                var km = haversine(lat, lng, cLat, cLng);
                card.dataset.distance = km.toString();
                label.textContent = '距離: ' + km.toFixed(2) + 'km';
            });

            cards.sort(function(a,b){
                var da = toNumber(a.dataset.distance);
                var db = toNumber(b.dataset.distance);
                if (da === null && db === null) return 0;
                if (da === null) return 1;
                if (db === null) return -1;
                return da - db;
            }).forEach(function(card){ list.appendChild(card); });
            currentPage = 1;
            apply();
        }

        if (input) input.addEventListener('input', function(){ currentPage = 1; apply(); });
        filters.forEach(function(el){ if (el) el.addEventListener('change', function(){ currentPage = 1; apply(); }); });
        if (prev) prev.addEventListener('click', function(){ if (currentPage > 1) { currentPage--; apply(); } });
        if (next) next.addEventListener('click', function(){ currentPage++; apply(); });

        if (geo) {
            geo.addEventListener('click', function(){
                if (!navigator.geolocation) { alert('位置情報が利用できません'); return; }
                navigator.geolocation.getCurrentPosition(function(pos){
                    applyDistance(pos.coords.latitude, pos.coords.longitude);
                }, function(){ alert('位置情報の取得に失敗しました'); });
            });
        }

        window.addEventListener('resize', function(){ apply(); });
        apply();
    }

    function boot(){
        document.querySelectorAll('.store-search-section').forEach(init);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
";
                head.AppendChild(script);
        }

    private static void InsertAfter(IElement newElement, IElement? target)
    {
        if (target is null || target.ParentElement is null)
        {
            return;
        }

        var parent = target.ParentElement;
        var nextSibling = target.NextSibling;
        if (nextSibling is null)
        {
            parent.AppendChild(newElement);
        }
        else
        {
            parent.InsertBefore(newElement, nextSibling);
        }
    }

    private static IElement CreateRankingSection(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var wrapper = document.CreateElement("div");
        wrapper.ClassName = "section-group ranking-section";
        wrapper.SetAttribute("data-section", "ranking");
        wrapper.InnerHtml = BuildRankingSectionHtml(content, template, overrides, embedImages);
        return wrapper;
    }

    private static IElement CreateStoreSearchSection(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages,
        string key)
    {
        var wrapper = document.CreateElement("div");
        wrapper.ClassName = "section-group store-search-section";
        wrapper.SetAttribute("data-section", key);
        wrapper.InnerHtml = BuildStoreSearchSectionHtml(content, template, overrides, embedImages);
        return wrapper;
    }

    private static IElement CreateCustomSection(
        IDocument document,
        CustomSectionModel section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var wrapper = document.CreateElement("div");
        wrapper.ClassName = "section-group custom-section";
        wrapper.SetAttribute("data-section", section.Key);
        wrapper.InnerHtml = BuildCustomSectionHtml(section, content, template, overrides, embedImages);
        return wrapper;
    }

    private static void UpdateRankingSection(
        IElement section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        section.ClassList.Add("ranking-section");
        section.InnerHtml = BuildRankingSectionHtml(content, template, overrides, embedImages);
    }

    private static void UpdateStoreSearchSection(
        IElement section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        section.ClassList.Add("store-search-section");
        section.InnerHtml = BuildStoreSearchSectionHtml(content, template, overrides, embedImages);
    }

    private static void UpdateCustomSection(
        IElement section,
        CustomSectionModel custom,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        section.ClassList.Add("custom-section");
        section.InnerHtml = BuildCustomSectionHtml(custom, content, template, overrides, embedImages);
    }

    private static string BuildRankingSectionHtml(
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        Debug.WriteLine($"[Preview] NotesItems={content.Sections.Ranking.NotesItems?.Count} text={string.Join("|", content.Sections.Ranking.NotesItems?.Select(x => x.Text) ?? Enumerable.Empty<string>())}");
        var ranking = content.Sections.Ranking;
        var title = ranking.Title ?? string.Empty;
        var subtitle = ranking.Subtitle ?? string.Empty;
        var headerLabels = ranking.HeaderLabels
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label.Trim())
            .ToList();
        if (headerLabels.Count == 0)
        {
            headerLabels = new List<string> { "順位", "決済金額" };
        }
        if (headerLabels.Count > 3)
        {
            headerLabels = headerLabels.Take(3).ToList();
        }

        var leftImg = string.IsNullOrWhiteSpace(ranking.ImageLeft) || IsImageDeleted(content, ranking.ImageLeft)
            ? string.Empty
            : $"<img src=\"{ResolveImageUrl(ranking.ImageLeft, template, overrides, embedImages)}\" alt=\"\" />";
        var rightImg = string.IsNullOrWhiteSpace(ranking.ImageRight) || IsImageDeleted(content, ranking.ImageRight)
            ? string.Empty
            : $"<img src=\"{ResolveImageUrl(ranking.ImageRight, template, overrides, embedImages)}\" alt=\"\" />";

        var rows = ranking.Rows.Count == 0
            ? new List<RankingRowModel>
            {
                new() { Rank = "1", Amount = "368,330円", Items = "940品以上" },
                new() { Rank = "2", Amount = "308,000円", Items = "790品以上" },
                new() { Rank = "3", Amount = "246,940円", Items = "630品以上" }
            }
            : ranking.Rows;

        var headerCells = string.Join("", headerLabels.Select(label => $"<th>{WebUtility.HtmlEncode(label)}</th>"));
        var colGroupHtml = headerLabels.Count switch
        {
            3 => "<colgroup><col style=\"width:22%\" /><col style=\"width:39%\" /><col style=\"width:39%\" /></colgroup>",
            2 => "<colgroup><col style=\"width:35%\" /><col style=\"width:65%\" /></colgroup>",
            _ => "<colgroup><col style=\"width:100%\" /></colgroup>"
        };

        var bodyRows = string.Join("", rows.Select((row, rowIndex) =>
        {
            var cells = new List<string>();
            for (var i = 0; i < headerLabels.Count; i++)
            {
                var cellValue = WebUtility.HtmlEncode(GetRankingCellValue(row, i));
                var isTop = ranking.ShowCrowns && i == 0 && rowIndex < 3;
                var tdClass = ranking.ShowCrowns && i == 0 && rowIndex < 3
                    ? $"king king-0{rowIndex + 1}--number"
                    : string.Empty;
                if (isTop)
                {
                    cellValue = $"<span class=\"king-rank-text\">{rowIndex + 1}</span>";
                }
                cells.Add(string.IsNullOrWhiteSpace(tdClass) ? $"<td>{cellValue}</td>" : $"<td class=\"{tdClass}\">{cellValue}</td>");
            }
            return $"<tr>{string.Join(string.Empty, cells)}</tr>";
        }));

        var textsHtml = RenderTextLines(ranking.FreeTexts, "ranking-free-texts");
        var subtitleUnderHtml = RenderTextLines(ranking.SubtitleUnderItems, "ranking-subtitle-under");
        var noteLines = RenderTextLineItems(ranking.NotesItems);

        var titleLines = RenderTextLineItems(ranking.TitleLines);
        if (titleLines.Count == 0 && !string.IsNullOrWhiteSpace(title))
        {
            titleLines.Add(BuildStyledTextLine(new StyledTextItem { Text = title, Bold = true, Align = "center" }));
        }

        var subtitleLines = RenderTextLineItems(ranking.SubtitleLines);
        if (subtitleLines.Count == 0 && !string.IsNullOrWhiteSpace(subtitle))
        {
            subtitleLines.Add(BuildStyledTextLine(new StyledTextItem { Text = subtitle, Align = "center" }));
        }

        var titleHtml = titleLines.Count == 0
            ? string.Empty
            : $"<div class=\"campaign__heading -white ranking-title-lines\">{string.Join(string.Empty, titleLines)}</div>";
        var subtitleHtml = subtitleLines.Count == 0
            ? string.Empty
            : $"<div class=\"ranking-subtitle-lines\">{string.Join(string.Empty, subtitleLines)}</div>";

        var tableNotesHtml = noteLines.Count == 0
            ? string.Empty
            : $"<div class=\"ranking-table-notes\">{string.Join(string.Empty, noteLines)}</div>";

        var tableWidthPercent = Math.Clamp(ranking.TableWidthPercent ?? 100, 60, 100);

		return $@"
<section class=""campaign"">
    <div class=""l-page-contents"">
        <div class=""campaign__block"">
            <div class=""campaign__box"" style=""width: {tableWidthPercent}%; margin: 0 auto;"">
                {titleHtml}
                <div class=""campaign__inner"">
          {subtitleHtml}
                    {subtitleUnderHtml}
                    {textsHtml}
                    
          {(string.IsNullOrWhiteSpace(leftImg) && string.IsNullOrWhiteSpace(rightImg) ? string.Empty : $"<div class=\"ranking-images\">{leftImg}{rightImg}</div>")}
                    <table class=""ranking-table campaign_rank-box"" style=""width: {tableWidthPercent}%; margin: 0 auto;"">
                        {colGroupHtml}
            <thead>
                              <tr>{headerCells}</tr>
            </thead>
            <tbody>
              {bodyRows}
            </tbody>
          </table>
                    {tableNotesHtml}
        </div>
      </div>
    </div>
  </div>
</section>";
    }

    private static string BuildStyledTextLine(StyledTextItem item, string tagName = "div", string? className = "ranking-text-line")
    {
        var text = WebUtility.HtmlEncode(item.Text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var styleBuilder = new StringBuilder();
        if (item.FontSize is > 0)
        {
            styleBuilder.Append($"font-size:{item.FontSize}px;");
        }
        if (item.Bold)
        {
            styleBuilder.Append("font-weight:700;");
        }
        var color = SanitizeCssColor(item.Color);
        if (!string.IsNullOrWhiteSpace(color))
        {
            styleBuilder.Append($"color:{color};");
        }
        var align = item.Align?.ToLowerInvariant() switch
        {
            "left" => "left",
            "right" => "right",
            _ => "center"
        };
        styleBuilder.Append($"text-align:{align};");
        var font = SanitizeFontFamily(item.FontFamily);
        if (!string.IsNullOrWhiteSpace(font))
        {
            styleBuilder.Append($"font-family:{font};");
        }
        var classAttr = string.IsNullOrWhiteSpace(className) ? string.Empty : $" class=\"{className}\"";
        return $"<{tagName}{classAttr} style=\"{styleBuilder}\">{text}</{tagName}>";
    }

    private static List<string> RenderTextLineItems(IEnumerable<StyledTextItem>? items, string tagName = "div", string? className = "ranking-text-line")
    {
        return (items ?? Enumerable.Empty<StyledTextItem>())
            .Where(item => item.Visible && !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => BuildStyledTextLine(item, tagName, className))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string RenderTextLines(IEnumerable<StyledTextItem>? items, string containerClass)
    {
        var lines = RenderTextLineItems(items);
        return lines.Count == 0
            ? string.Empty
            : $"<div class=\"{containerClass}\">{string.Join(string.Empty, lines)}</div>";
    }

	private static string BuildStoreSearchSectionHtml(
		ContentModel content,
		TemplateProject template,
		IDictionary<string, byte[]>? overrides,
		bool embedImages)
	{
        var titleValue = string.IsNullOrWhiteSpace(content.Sections.StoreSearch.Title)
            ? "キャンペーン対象店舗検索"
            : content.Sections.StoreSearch.Title;
        var noticeTitleValue = string.IsNullOrWhiteSpace(content.Sections.StoreSearch.NoticeTitle)
            ? "⚠️ ご注意ください！"
            : content.Sections.StoreSearch.NoticeTitle;
        var title = WebUtility.HtmlEncode(titleValue);
        var noticeTitle = WebUtility.HtmlEncode(noticeTitleValue);
		var noticeItems = content.Sections.StoreSearch.NoticeItems.Count == 0
			? new List<TextItemModel>
			{
                new() { Text = "リストに記載があっても、店舗の休業・閉業・移転や、その他の事情により利用できない場合があります。", Emphasis = false },
                new() { Text = "キャンペーン対象店舗であっても、一部掲載していない店舗もございます。", Emphasis = false },
                new() { Text = "データ連携のタイムラグ等により、キャッシュレス決済アプリ内の情報と一部異なる場合があります。", Emphasis = false },
                new() { Text = "店舗は随時追加・更新いたします。", Emphasis = false },
                new() { Text = "一部対象外商品、サービスがあります。", Emphasis = false }
			}
			: content.Sections.StoreSearch.NoticeItems;
		var noticeList = string.Join("", noticeItems.Select(item => $"<li>{ToStyledHtml(item)}</li>"));

        var stores = content.Sections.StoreSearch.Stores?.ToList() ?? new List<LPEditorApp.Models.StoreItemModel>();
        var labels = content.Sections.StoreSearch.TargetLabels
            .Where(label => !string.IsNullOrWhiteSpace(label.Key))
            .ToList();
        if (labels.Count == 0)
        {
            labels = new List<StoreTargetLabelModel>
            {
                new() { Key = "raffle", Name = "抽選対象" },
                new() { Key = "coupon", Name = "クーポン対象" },
                new() { Key = "campaign", Name = "キャンペーン対象" }
            };
        }

        var storeCards = string.Join("", stores.Select((store, index) =>
        {
            var labelHtml = BuildStoreLabels(store, labels);
            var lat = store.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var lng = store.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var targetKeys = string.Join("|", GetStoreTargetKeys(store, labels));
            var mapQuery = WebUtility.UrlEncode(string.Join(" ", new[] { store.Name, store.Address }.Where(text => !string.IsNullOrWhiteSpace(text))));
            var mapUrl = $"https://www.google.com/maps/search/?api=1&query={mapQuery}";
            return $"<div class=\"store-search-card\" data-index=\"{index}\" data-id=\"{WebUtility.HtmlEncode(store.Id)}\" data-name=\"{WebUtility.HtmlEncode(store.Name)}\" data-zip=\"{WebUtility.HtmlEncode(store.Zip)}\" data-address=\"{WebUtility.HtmlEncode(store.Address)}\" data-targets=\"{WebUtility.HtmlEncode(targetKeys)}\" data-lat=\"{lat}\" data-lng=\"{lng}\"><div class=\"store-search-card-title\"><a class=\"store-search-map-link\" href=\"{mapUrl}\" target=\"_blank\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(store.Name)}</a></div>{labelHtml}<div class=\"store-search-card-address\">{WebUtility.HtmlEncode(store.Zip)}<br />{WebUtility.HtmlEncode(store.Address)}</div><div class=\"store-search-distance\"></div></div>";
        }));

		return $@"
<section class=""campaign"">
    <div class=""l-page-contents"">
        <div class=""campaign__block"">
            <div class=""campaign__box"">
                <h2 class=""campaign__heading -white"">{title}</h2>
                <div class=""store-search-body"">
                    <div class=""store-search-notice"">
                        {(string.IsNullOrWhiteSpace(noticeTitle) ? string.Empty : $"<div class=\"store-search-notice-heading\">{noticeTitle}</div>")}
                        <ul class=""store-search-notice-list"">{noticeList}</ul>
                    </div>
                    <div class=""store-search-search"">
                        <div class=""store-search-filters"">
                            {BuildStoreFilterHtml(labels)}
                        </div>
                        <div class=""store-search-input"">
                            <input type=""text"" class=""store-search-keyword"" placeholder=""キーワードを入力してください"" />
                            <button type=""button"" class=""store-search-geo"">現在地から検索</button>
                        </div>
                    </div>
                    <div class=""store-search-result"">該当件数：{stores.Count}件</div>
                    <div class=""store-search-pager"">
                        <button type=""button"" class=""store-search-prev"">前へ</button>
                        <span class=""store-search-page-info"">1 / 1</span>
                        <button type=""button"" class=""store-search-next"">次へ</button>
                    </div>
                    <div class=""store-search-list"">
                        {storeCards}
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>";
	}

	private static string BuildCustomSectionHtml(
		CustomSectionModel section,
		ContentModel content,
		TemplateProject template,
		IDictionary<string, byte[]>? overrides,
		bool embedImages)
	{
		var title = WebUtility.HtmlEncode(section.Title ?? string.Empty);
		var bodyHtml = RenderTextLines(section.BodyTextItems, "custom-section-body-lines");
		var notesHtml = RenderTextLines(section.ImageNotesItems, "custom-section-notes");
		var imageHtml = string.Empty;
		if (!string.IsNullOrWhiteSpace(section.ImagePath) && !IsImageDeleted(content, section.ImagePath))
		{
			var imgSrc = ResolveImageUrl(section.ImagePath, template, overrides, embedImages);
			var alt = WebUtility.HtmlEncode(section.ImageAlt ?? string.Empty);
			imageHtml = $"<div class=\"custom-section-image\"><img src=\"{imgSrc}\" alt=\"{alt}\" /></div>";
		}
		var linkHtml = string.Empty;
		if (!string.IsNullOrWhiteSpace(section.LinkUrl) && IsValidUrl(section.LinkUrl))
		{
			var url = WebUtility.HtmlEncode(section.LinkUrl);
			linkHtml = $"<div class=\"custom-section-link\"><a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">詳細を見る</a></div>";
		}

		return $@"
<section class=""campaign"">
  <div class=""l-page-contents"">
    <div class=""campaign__block"">
      <div class=""campaign__box"">
        <h2 class=""campaign__heading -white"">{title}</h2>
        <div class=""custom-section-body"">
          {bodyHtml}
          {imageHtml}
          {notesHtml}
          {linkHtml}
        </div>
      </div>
    </div>
  </div>
</section>";
	}

	private static bool IsValidUrl(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}

		return Uri.TryCreate(url, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}
        private static string BuildStoreLabels(StoreItemModel store, List<StoreTargetLabelModel> labels)
        {
            var labelHtml = new List<string>();
            foreach (var label in labels.Where(l => l.IsEnabled))
            {
                if (IsStoreTarget(store, label.Key))
                {
                    labelHtml.Add($"<span class=\"store-label\" style=\"{LabelStyleService.BuildInlineStyle(label)}\">{WebUtility.HtmlEncode(label.Name)}</span>");
                }
            }

            return labelHtml.Count == 0 ? string.Empty : $"<div class=\"store-labels\">{string.Join(string.Empty, labelHtml)}</div>";
        }

        private static IEnumerable<string> GetStoreTargetKeys(StoreItemModel store, List<StoreTargetLabelModel> labels)
        {
            foreach (var label in labels.Where(l => l.IsEnabled))
            {
                if (IsStoreTarget(store, label.Key))
                {
                    yield return label.Key;
                }
            }
        }

        private static bool IsStoreTarget(StoreItemModel store, string key)
        {
            if (store.Targets is not null && store.Targets.TryGetValue(key, out var isTarget))
            {
                return isTarget;
            }

            return IsLegacyTarget(store, key);
        }

        private static bool IsLegacyTarget(StoreItemModel store, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var normalized = key.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (normalized.Contains("抽選", StringComparison.OrdinalIgnoreCase) || normalized.Equals("raffle", StringComparison.OrdinalIgnoreCase))
            {
                return store.RaffleTarget;
            }
            if (normalized.Contains("クーポン", StringComparison.OrdinalIgnoreCase) || normalized.Equals("coupon", StringComparison.OrdinalIgnoreCase))
            {
                return store.CouponTarget;
            }
            if (normalized.Contains("キャンペーン", StringComparison.OrdinalIgnoreCase) || normalized.Equals("campaign", StringComparison.OrdinalIgnoreCase))
            {
                return store.CampaignTarget;
            }

            return false;
        }

        private static string BuildStoreFilterHtml(List<StoreTargetLabelModel> labels)
        {
            if (labels.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("", labels.Where(label => label.IsEnabled).Select(label =>
                $"<label><input type=\"checkbox\" data-filter-key=\"{WebUtility.HtmlEncode(label.Key)}\" checked /> {WebUtility.HtmlEncode(label.Name)}</label>"));
        }
    private static void ApplySectionGroups(IDocument document, TemplateProject template, ContentModel content)
    {
        var stateList = content.SectionGroups?.Where(group => !string.IsNullOrWhiteSpace(group.Key)).ToList();
        if (stateList is null || stateList.Count == 0)
        {
            return;
        }

        var orderKeys = stateList.Select(group => group.Key).Where(key => !string.IsNullOrWhiteSpace(key)).ToList();
        var fixedBottomKeys = orderKeys
            .Where(key => key.Contains("conditions-contact-banners", StringComparison.OrdinalIgnoreCase))
            .ToList();
        orderKeys = orderKeys.Where(key => IsEditorManagedSectionKey(key, content)).ToList();
        var orderLog = string.Join(", ", orderKeys);
        System.Diagnostics.Debug.WriteLine($"[Preview] preview build order: {orderLog}");
        Console.WriteLine($"[Preview] preview build order: {orderLog}");

        if (orderKeys.Count == 0)
        {
            return;
        }

        var enabledMap = stateList.ToDictionary(group => group.Key, group => group.Enabled, StringComparer.OrdinalIgnoreCase);

        var entries = orderKeys.Concat(fixedBottomKeys)
            .SelectMany(key => GetElementsForSectionKey(document, key)
                .Select(element => new { Key = key, Element = element }))
            .Where(item => item.Element.ParentElement is not null)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var groupsByParent = entries
            .GroupBy(item => item.Element.ParentElement)
            .Where(group => group.Key is not null)
            .ToList();

        foreach (var parentGroup in groupsByParent)
        {
            var parent = parentGroup.Key!;
            var items = parentGroup.ToList();

            foreach (var item in items)
            {
                if (enabledMap.TryGetValue(item.Key, out var enabled) && !enabled)
                {
                    item.Element.Remove();
                    continue;
                }

                parent.RemoveChild(item.Element);
            }

            foreach (var key in orderKeys)
            {
                if (enabledMap.TryGetValue(key, out var enabled) && !enabled)
                {
                    continue;
                }

                foreach (var item in items.Where(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)))
                {
                    parent.AppendChild(item.Element);
                }
            }

            foreach (var key in fixedBottomKeys)
            {
                if (enabledMap.TryGetValue(key, out var enabled) && !enabled)
                {
                    continue;
                }

                foreach (var item in items.Where(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)))
                {
                    parent.AppendChild(item.Element);
                }
            }

            var fixedWrapper = parent.QuerySelector(".section-group[data-section='conditions-contact-banners']");
            if (fixedWrapper is not null)
            {
                fixedWrapper.Remove();
                parent.AppendChild(fixedWrapper);
            }
        }
    }

    private static IEnumerable<IElement> GetElementsForSectionKey(IDocument document, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Enumerable.Empty<IElement>();
        }

        if (key.Contains("conditions-contact-banners", StringComparison.OrdinalIgnoreCase))
        {
            var wrapper = document.QuerySelector(".section-group[data-section='conditions-contact-banners']");
            if (wrapper is not null)
            {
                return new[] { wrapper };
            }

            var list = new List<IElement>();
            list.AddRange(document.QuerySelectorAll("section.conditions").ToList());
            list.AddRange(document.QuerySelectorAll("section.contact").ToList());
            list.AddRange(document.QuerySelectorAll(".banner-head").ToList());
            list.AddRange(document.QuerySelectorAll("a.banner").ToList());
            list.AddRange(document.QuerySelectorAll(".magazine").ToList());
            return list;
        }

        var normalizedKey = NormalizeSectionKey(key);
        var groups = document.QuerySelectorAll(".section-group[data-section]").ToList();
        return groups.Where(group => NormalizeSectionKey(group.GetAttribute("data-section")) == normalizedKey).ToList();
    }

    private static bool IsEditorManagedSectionKey(string? key, ContentModel content)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (content.CustomSections is not null
            && content.CustomSections.Any(section => string.Equals(section.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return NormalizeSectionKey(key) switch
        {
            "campaigncontent" => true,
            "couponperiod" => true,
            "couponnotes" => true,
            "ranking" => true,
            "countdown" => true,
            "storesearch" => true,
            _ => false
        };
    }

    private static string NormalizeSectionKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return Regex.Replace(key.Trim().ToLowerInvariant(), "[^a-z0-9]", string.Empty);
    }


    private static void PlaceSectionsAfterMvFooter(
        ContentModel content,
        IDocument document,
        IElement? campaignContentSection,
        IElement? couponNotesSection,
        IElement? conditionsSection,
        IElement? contactSection)
    {
        if (campaignContentSection is null
            && couponNotesSection is null
            && conditionsSection is null
            && contactSection is null)
        {
            return;
        }

        var parent = conditionsSection?.ParentElement
            ?? campaignContentSection?.ParentElement
            ?? couponNotesSection?.ParentElement
            ?? contactSection?.ParentElement;
        if (parent is null)
        {
            return;
        }

        return;

    }

    private static void SetText(IParentNode root, string? text, params string[] selectors)
    {
        var target = FindFirst(root, selectors);
        if (target is null)
        {
            return;
        }

        target.TextContent = text ?? string.Empty;
    }

    private static void SetHtmlText(IParentNode root, string? text, params string[] selectors)
    {
        var target = FindFirst(root, selectors);
        if (target is null)
        {
            return;
        }

        var html = WebUtility.HtmlEncode(text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);

        target.InnerHtml = html;
    }

    private static void SetList(IParentNode root, IEnumerable<TextItemModel> items, params string[] selectors)
    {
        var target = FindFirst(root, selectors);
        if (target is null)
        {
            return;
        }

        var listHtml = string.Join("", items.Select(item => $"<li>{ToStyledHtml(item)}</li>"));
        target.InnerHtml = listHtml;
    }

    private static void SetStyledList(IParentNode root, IEnumerable<StyledTextItem> items, params string[] selectors)
    {
        var target = FindFirst(root, selectors);
        if (target is null)
        {
            return;
        }

        var listHtml = string.Join("", RenderTextLineItems(items, "li", null));
        target.InnerHtml = listHtml;
    }

    private static List<StyledTextItem> MapTextItemsToStyledLines(IEnumerable<TextItemModel> items, string? fontFamily, string? align)
    {
        var safeAlign = string.IsNullOrWhiteSpace(align) ? "center" : align;
        return items.Select(item => new StyledTextItem
            {
                Text = item.Text ?? string.Empty,
                Bold = item.Bold,
                Visible = true,
                FontSize = item.FontSize,
                Color = item.UseColor ? item.Color : null,
                FontFamily = fontFamily,
                Align = safeAlign
            })
            .ToList();
    }

    private static void UpsertFooterTextLines(IDocument document, IElement section, IEnumerable<StyledTextItem>? items)
    {
        var lines = RenderTextLineItems(items, "div", "footer-text-line");
        var existing = section.QuerySelector(".footer-text-lines");
        if (lines.Count == 0)
        {
            existing?.Remove();
            return;
        }

        if (existing is null)
        {
            var container = document.CreateElement("div");
            container.ClassName = "footer-text-lines";
            section.AppendChild(container);
            existing = container;
        }

        existing.InnerHtml = string.Join(string.Empty, lines);
    }

    private static void SetContactButtons(IElement contactSection, IEnumerable<ButtonItemModel> items, string? officeHours)
    {
        var wrap = contactSection.QuerySelector(".contact__wrap");
        if (wrap is null)
        {
            return;
        }

        var listHtml = new List<string>();
        var index = 0;
        foreach (var item in items)
        {
            var label = WebUtility.HtmlEncode(item.Label ?? string.Empty);
            var url = WebUtility.HtmlEncode(item.Url ?? string.Empty);
            if (item.Emphasis)
            {
                label = $"<span class=\"is-emphasis\">{label}</span>";
            }

            var detailText = string.Empty;
            if (index == 0 && !string.IsNullOrWhiteSpace(officeHours))
            {
                detailText = WebUtility.HtmlEncode(officeHours)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace("\n", "<br />", StringComparison.Ordinal);
            }

            listHtml.Add($"<li class=\"contact__item\"><a href=\"{url}\" target=\"_blank\" class=\"c-button\">{label}</a><p class=\"contact__text\">{detailText}</p></li>");
            index++;
        }

        wrap.InnerHtml = string.Join("", listHtml);
    }

    private static string ToStyledHtml(TextItemModel item)
    {
        var text = WebUtility.HtmlEncode(item.Text ?? string.Empty);
        return BuildTextSpan(text, item.Emphasis, item.Bold, item.UseColor, item.Color, item.FontSize);
    }

    private static string ToStyledHtml(RankingTextItemModel item)
    {
        var text = WebUtility.HtmlEncode(item.Text ?? string.Empty);
        return BuildTextSpan(text, item.Emphasis, item.Bold, item.UseColor, item.Color, item.FontSize);
    }

    private static string BuildTextSpan(string text, bool emphasis, bool bold, bool useColor, string? color, int? fontSize)
    {
        var classNames = new List<string>();
        if (emphasis)
        {
            classNames.Add("is-emphasis");
        }

        var styles = new List<string>();
        if (bold)
        {
            styles.Add("font-weight:700");
        }

        if (useColor)
        {
            var safeColor = SanitizeCssColor(color);
            if (!string.IsNullOrWhiteSpace(safeColor))
            {
                styles.Add($"color:{safeColor}");
            }
        }

        var safeSize = SanitizeFontSize(fontSize);
        if (safeSize is not null)
        {
            styles.Add($"font-size:{safeSize}px");
        }

        if (classNames.Count == 0 && styles.Count == 0)
        {
            return text;
        }

        var classAttr = classNames.Count == 0 ? string.Empty : $" class=\"{string.Join(" ", classNames)}\"";
        var styleAttr = styles.Count == 0 ? string.Empty : $" style=\"{string.Join(";", styles)}\"";
        return $"<span{classAttr}{styleAttr}>{text}</span>";
    }

    private static int? SanitizeFontSize(int? fontSize)
    {
        if (!fontSize.HasValue)
        {
            return null;
        }

        var value = fontSize.Value;
        return value is >= 10 and <= 32 ? value : null;
    }

    private static IElement? FindFirst(IParentNode root, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var element = root.QuerySelector(selector);
            if (element is not null)
            {
                return element;
            }
        }

        return null;
    }

    private static string ResolveImageUrl(
        string path,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        if (!embedImages)
        {
            return path;
        }

        if (overrides is not null && overrides.TryGetValue(path, out var overrideBytes))
        {
            return ToDataUrlOrNull(overrideBytes, path) ?? path;
        }

        if (template.Files.TryGetValue(path, out var file) && file.IsImage)
        {
            return ToDataUrlOrNull(file.Data, path) ?? path;
        }

        return path;
    }

    private static bool IsImageDeleted(ContentModel content, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return content.DeletedImages.Any(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatDateRange(string? startDate, string? endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
        {
            return string.Empty;
        }

        return $"{start:yyyy年M月d日}～{end:yyyy年M月d日}";
    }

    private static string GetRankingCellValue(RankingRowModel row, int index)
    {
        return index switch
        {
            0 => row.Rank,
            1 => row.Amount,
            2 => row.Items,
            _ => string.Empty
        };
    }

    private static string ResolveCouponPeriodText(ContentModel content)
    {
        var mode = content.Sections.CouponPeriod.InputMode ?? "manual";
        if (string.Equals(mode, "link", StringComparison.OrdinalIgnoreCase))
        {
            return FormatDateRange(content.Campaign.StartDate, content.Campaign.EndDate);
        }

        if (string.Equals(mode, "calendar", StringComparison.OrdinalIgnoreCase))
        {
            return FormatDateRange(content.Sections.CouponPeriod.StartDate, content.Sections.CouponPeriod.EndDate);
        }

        return content.Sections.CouponPeriod.Text ?? string.Empty;
    }

    private static void ApplyCountdownDisplay(IDocument document, ContentModel content, string dateRangeText)
    {
        if (!content.Campaign.ShowCountdown)
        {
            foreach (var element in document.All.Where(el => el.ClassList.Any(cls => cls.Contains("countdown", StringComparison.OrdinalIgnoreCase))).ToList())
            {
                if (element.ClassList.Contains("countdown-period") || element.QuerySelector(".countdown-period") is not null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(element.TextContent)
                    && element.TextContent.Contains("キャンペーン期間", StringComparison.Ordinal))
                {
                    continue;
                }

                element.SetAttribute("data-countdown-hidden", "true");
            }

            EnsureCountdownHiddenStyle(document);
            UpdateCountdownPeriodText(document, dateRangeText);
            return;
        }

        foreach (var element in document.QuerySelectorAll("[data-countdown-hidden='true']").ToList())
        {
            element.RemoveAttribute("data-countdown-hidden");
        }

        UpdateCountdownPeriodText(document, dateRangeText);
    }

    private static void UpdateCountdownPeriodText(IDocument document, string dateRangeText)
    {
        if (string.IsNullOrWhiteSpace(dateRangeText))
        {
            return;
        }

        foreach (var element in document.QuerySelectorAll(".countdown-period, .countdown-period__text, .campaign-period, .campaign__period"))
        {
            element.TextContent = dateRangeText;
        }
    }

    private static void EnsureCountdownHiddenStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null || document.QuerySelector("style[data-countdown-hidden-style='true']") is not null)
        {
            return;
        }

        var style = document.CreateElement("style");
        style.SetAttribute("data-countdown-hidden-style", "true");
        style.TextContent = @"
[data-countdown-hidden='true'] { display: none !important; }
";
        head.AppendChild(style);
    }

    private static void EnsureCampaignEndedOverlay(IDocument document, ContentModel content)
    {
        var endAt = GetCampaignEndDateTime(content);
        if (endAt is null || endAt > DateTime.Now || content.Campaign.ShowCountdown)
        {
            document.QuerySelector("[data-campaign-ended='true']")?.Remove();
            return;
        }

        var message = string.IsNullOrWhiteSpace(content.Campaign.EndedMessage)
            ? "キャンペーン終了しました"
            : content.Campaign.EndedMessage;

        var head = document.Head;
        if (head is not null && document.QuerySelector("style[data-campaign-ended-style='true']") is null)
        {
            var style = document.CreateElement("style");
            style.SetAttribute("data-campaign-ended-style", "true");
            style.TextContent = @"
.campaign-ended-overlay { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.45); backdrop-filter: blur(6px); display: flex; align-items: center; justify-content: center; z-index: 9999; }
.campaign-ended-card { background: #ffffff; border-radius: 20px; padding: 28px 36px; box-shadow: 0 30px 60px rgba(15, 23, 42, 0.3); border: 1px solid rgba(15, 23, 42, 0.15); text-align: center; position: relative; min-width: 280px; }
.campaign-ended-title { font-size: 1.4rem; font-weight: 800; color: #b91c1c; margin-bottom: 8px; }
.campaign-ended-sub { font-size: 0.95rem; color: #475569; }
.campaign-ended-close { position: absolute; top: 10px; right: 10px; width: 32px; height: 32px; border-radius: 999px; border: 1px solid rgba(15, 23, 42, 0.2); background: #f8fafc; color: #0f172a; font-weight: 700; cursor: pointer; }
.campaign-ended-close:hover { background: #e2e8f0; }
";
            head.AppendChild(style);
        }

        var overlay = document.QuerySelector("[data-campaign-ended='true']") as IElement;
        if (overlay is null)
        {
            overlay = document.CreateElement("div");
            overlay.SetAttribute("data-campaign-ended", "true");
            overlay.ClassName = "campaign-ended-overlay";
            overlay.InnerHtml = $"<div class=\"campaign-ended-card\"><button class=\"campaign-ended-close\" type=\"button\" aria-label=\"閉じる\" onclick=\"this.closest('[data-campaign-ended]')?.remove()\">×</button><div class=\"campaign-ended-title\">{WebUtility.HtmlEncode(message)}</div><div class=\"campaign-ended-sub\">またのご利用をお待ちしています</div></div>";
            document.Body?.AppendChild(overlay);
        }
        else
        {
            var title = overlay.QuerySelector(".campaign-ended-title");
            if (title is not null)
            {
                title.TextContent = message;
            }
        }
    }

    private static DateTime? GetCampaignEndDateTime(ContentModel content)
    {
        if (DateTime.TryParse(content.Campaign.CountdownEnd, out var countdownEnd))
        {
            return countdownEnd;
        }

        if (DateTime.TryParse(content.Campaign.EndDate, out var endDate))
        {
            return endDate.Date.AddDays(1).AddSeconds(-1);
        }

        return null;
    }

    private static void InlineStyles(
        IDocument document,
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var links = document.QuerySelectorAll("link[rel='stylesheet'][href]").ToList();
        foreach (var link in links)
        {
            var href = link.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (IsExternalUrl(href))
            {
                continue;
            }

            var resolvedPath = ResolveTemplatePath(template, href);
            if (resolvedPath is null || !template.Files.TryGetValue(resolvedPath, out var cssFile))
            {
                continue;
            }

            var cssText = System.Text.Encoding.UTF8.GetString(cssFile.Data);
            var baseDir = GetDirectoryPath(resolvedPath);
            cssText = NormalizeMediaQueries(cssText);
            if (embedImages)
            {
                cssText = InlineCssUrls(cssText, baseDir, template, content, overrides);
            }

            var style = document.CreateElement("style");
            style.SetAttribute("data-source", resolvedPath);
            style.TextContent = cssText;
            link.Replace(style);
        }
    }

    private static void InlineScripts(IDocument document, TemplateProject template, ContentModel content)
    {
        var scripts = document.QuerySelectorAll("script[src]").ToList();
        var replacer = new JsReplacementService();
        foreach (var script in scripts)
        {
            var src = script.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src))
            {
                continue;
            }

            if (IsExternalUrl(src))
            {
                continue;
            }

            var resolvedPath = ResolveTemplatePath(template, src);
            if (resolvedPath is null || !template.Files.TryGetValue(resolvedPath, out var jsFile))
            {
                continue;
            }

            var jsText = System.Text.Encoding.UTF8.GetString(jsFile.Data);
            jsText = replacer.ReplaceCountdownEnd(jsText, content.Campaign.CountdownEnd);
            var newScript = document.CreateElement("script");
            var type = script.GetAttribute("type");
            if (!string.IsNullOrWhiteSpace(type))
            {
                newScript.SetAttribute("type", type);
            }

            newScript.TextContent = jsText;
            script.Replace(newScript);
        }
    }

    private static void ReplaceCountdownEndInScripts(IDocument document, ContentModel content)
    {
        var replacer = new JsReplacementService();
        foreach (var script in document.QuerySelectorAll("script").ToList())
        {
            if (string.IsNullOrWhiteSpace(script.TextContent))
            {
                continue;
            }

            script.TextContent = replacer.ReplaceCountdownEnd(script.TextContent, content.Campaign.CountdownEnd);
        }
    }

    private static void EmbedImageSources(
        IDocument document,
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        if (!embedImages)
        {
            return;
        }

        foreach (var img in document.QuerySelectorAll("img[src]").ToList())
        {
            var src = img.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src) || IsExternalUrl(src))
            {
                continue;
            }

            var resolvedPath = ResolveTemplatePath(template, src);
            if (resolvedPath is null)
            {
                continue;
            }

            byte[]? overrideBytes = null;
            if (overrides is not null)
            {
                overrides.TryGetValue(resolvedPath, out overrideBytes);
            }
            var overrideLen = overrideBytes?.Length ?? 0;
            var fileLen = template.Files.TryGetValue(resolvedPath, out var fileInfo) ? fileInfo.Data?.Length ?? 0 : 0;
            var isImage = fileInfo?.IsImage ?? false;
            LogPreviewImageInfo($"img src={src}", resolvedPath, overrideLen, isImage, fileLen);

            if (IsImageDeleted(content, resolvedPath))
            {
                img.RemoveAttribute("src");
                continue;
            }

            if (overrides is not null && overrideBytes is not null)
            {
                var dataUrl = ToDataUrlOrNull(overrideBytes, resolvedPath);
                if (!string.IsNullOrWhiteSpace(dataUrl))
                {
                    img.SetAttribute("src", dataUrl);
                }
                else
                {
                    LogPreviewImageWarn($"override bytes empty for {resolvedPath}");
                }
                continue;
            }

            if (fileInfo is not null && isImage)
            {
                var dataUrl = ToDataUrlOrNull(fileInfo.Data, resolvedPath);
                if (!string.IsNullOrWhiteSpace(dataUrl))
                {
                    img.SetAttribute("src", dataUrl);
                }
                else
                {
                    LogPreviewImageWarn($"template bytes empty for {resolvedPath}");
                }
            }
        }

        foreach (var source in document.QuerySelectorAll("source[srcset]").ToList())
        {
            var srcset = source.GetAttribute("srcset");
            if (string.IsNullOrWhiteSpace(srcset))
            {
                continue;
            }

            var replaced = ReplaceSrcSet(srcset, template, content, overrides);
            if (!string.IsNullOrWhiteSpace(replaced))
            {
                source.SetAttribute("srcset", replaced);
                LogPreviewImageInfo($"srcset replaced head={GetPreviewHead(replaced)}", "(srcset)", 0, true, replaced.Length);
            }
        }
    }

    private static string ReplaceSrcSet(
        string srcset,
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]>? overrides)
    {
        if (srcset.Contains("data:", StringComparison.OrdinalIgnoreCase))
        {
            return srcset;
        }

        var candidates = srcset.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var converted = candidates.Select(entry =>
        {
            var trimmed = entry.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return entry;
            }

            var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var url = parts[0];
            var descriptor = parts.Length > 1 ? " " + parts[1] : string.Empty;
            if (IsExternalUrl(url))
            {
                return url + descriptor;
            }

            var resolvedPath = ResolveTemplatePath(template, url);
            if (resolvedPath is null)
            {
                return url + descriptor;
            }

            if (IsImageDeleted(content, resolvedPath))
            {
                return string.Empty;
            }

            if (overrides is not null && overrides.TryGetValue(resolvedPath, out var overrideBytes))
            {
                var dataUrl = ToDataUrlForSrcSetOrNull(overrideBytes, resolvedPath);
                return string.IsNullOrWhiteSpace(dataUrl) ? url + descriptor : dataUrl + descriptor;
            }

            if (template.Files.TryGetValue(resolvedPath, out var file) && file.IsImage)
            {
                var dataUrl = ToDataUrlForSrcSetOrNull(file.Data, resolvedPath);
                return string.IsNullOrWhiteSpace(dataUrl) ? url + descriptor : dataUrl + descriptor;
            }

            return url + descriptor;
        });

        return string.Join(", ", converted);
    }

    private static string InlineCssUrls(
        string cssText,
        string baseDir,
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]>? overrides)
    {
        return Regex.Replace(cssText, "url\\((?<url>[^)]+)\\)", match =>
        {
            var raw = match.Groups["url"].Value.Trim();
            var cleaned = raw.Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(cleaned) || IsExternalUrl(cleaned) || cleaned.StartsWith("#", StringComparison.Ordinal))
            {
                return match.Value;
            }

            var resolved = ResolveRelativePath(baseDir, cleaned);
            var resolvedPath = ResolveTemplatePath(template, resolved);
            if (resolvedPath is null)
            {
                return match.Value;
            }

            if (IsImageDeleted(content, resolvedPath))
            {
                return "none";
            }

            if (overrides is not null && overrides.TryGetValue(resolvedPath, out var overrideBytes))
            {
                var dataUrl = ToDataUrlOrNull(overrideBytes, resolvedPath);
                return string.IsNullOrWhiteSpace(dataUrl) ? match.Value : $"url('{dataUrl}')";
            }

            if (template.Files.TryGetValue(resolvedPath, out var file) && file.IsImage)
            {
                var dataUrl = ToDataUrlOrNull(file.Data, resolvedPath);
                return string.IsNullOrWhiteSpace(dataUrl) ? match.Value : $"url('{dataUrl}')";
            }

            return match.Value;
        });
    }

    private static string NormalizeMediaQueries(string cssText)
    {
        if (string.IsNullOrWhiteSpace(cssText))
        {
            return cssText;
        }

        var normalized = Regex.Replace(cssText, "\\b(min|max)-device-width\\b", "$1-width", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, "\\bdevice-width\\b", "width", RegexOptions.IgnoreCase);
        return normalized;
    }

    private static string ResolveRelativePath(string baseDir, string relativePath)
    {
        var normalizedBase = NormalizePath(baseDir);
        var baseUri = new Uri($"http://local/{normalizedBase.TrimEnd('/')}/");
        var resolved = new Uri(baseUri, relativePath);
        return resolved.AbsolutePath.TrimStart('/');
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace("\\", "/");
        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(2);
        }

        return normalized.TrimStart('/');
    }

    private static string? ResolveTemplatePath(TemplateProject template, string path)
    {
        var normalized = NormalizePath(path);
        if (template.Files.ContainsKey(normalized))
        {
            return normalized;
        }

        var suffixMatch = template.Files.Keys
            .FirstOrDefault(key => key.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(suffixMatch))
        {
            return suffixMatch;
        }

        return template.Files.Keys
            .FirstOrDefault(key => key.EndsWith(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetDirectoryPath(string path)
    {
        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized.Substring(0, lastSlash + 1) : string.Empty;
    }

    private static bool IsExternalUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("//", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ToDataUrlOrNull(byte[]? bytes, string path)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var mime = GetMimeTypeFromPath(path);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mime};base64,{base64}";
    }

    private static string? ToDataUrlForSrcSetOrNull(byte[]? bytes, string path)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var mime = GetMimeTypeFromPath(path);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mime};base64%2C{base64}";
    }

    private static void LogPreviewImageInfo(string source, string resolvedPath, int overrideLen, bool isImage, int fileLen)
    {
        var message = $"[Preview] {source} -> {resolvedPath} overrideLen={overrideLen} isImage={isImage} fileLen={fileLen}";
        System.Diagnostics.Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    private static void LogPreviewImageWarn(string message)
    {
        var line = $"[Preview][Warn] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }

    private static string GetPreviewHead(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 50 ? value : value.Substring(0, 50);
    }

    private static string GetMimeTypeFromPath(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string GetCrownIconUrl(
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages,
        int rank)
    {
        var filename = rank switch
        {
            1 => "icon-king-01.svg",
            2 => "icon-king-02.svg",
            3 => "icon-king-03.svg",
            _ => "icon-king-01.svg"
        };

        var resolved = ResolveTemplatePath(template, filename) ?? filename;
        return ResolveImageUrl(resolved, template, overrides, embedImages);
    }
}
