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
        bool embedImages,
        bool includeDebugStamp = false,
        string? editingSectionKey = null,
        int? fixedViewportWidth = null,
        bool applySpResetCss = false)
    {
        var startedAt = DateTime.Now;
        Debug.WriteLine($"[PreviewService] GenerateHtmlAsync start embedImages={embedImages} debug={includeDebugStamp} editing={editingSectionKey}");
        Console.WriteLine($"[PreviewService] GenerateHtmlAsync start embedImages={embedImages} debug={includeDebugStamp} editing={editingSectionKey}");
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
        EnsureViewportMeta(document, fixedViewportWidth);
        if (applySpResetCss)
        {
            EnsureSpResetStyle(document);
        }
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
        EnsurePaymentHistoryStyle(document, content);
        EnsureStoreSearchStyle(document, content);
        EnsureStoreSearchScript(document, content);
        EnsureCouponFlowStyle(document, content);
        EnsureCouponFlowScript(document, content);
        EnsureStickyTabsStyle(document, content);
        EnsureStickyTabsScript(document, content);
        EnsureCustomSectionGalleryStyle(document, content);
        EnsureCustomSectionGalleryScript(document, content);
        EnsureSectionSpacingStyle(document);
        EnsureTypographyScaleStyle(document, content);
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
        ApplySectionStyles(document, content, editingSectionKey);
        ApplySectionDecorations(document, content);
        ApplySectionAnimations(document, content);
        EnsureSectionBodyOnly(document, content);
        RenderCommonFrame(document, content);
        ApplyFrameAnimations(document, content);
        EnsureFrameAnimationStyle(document);
        EnsureFrameAnimationScript(document);
        EnsureSectionAnimationStyle(document);
        EnsureSectionAnimationScript(document);
        EnsureDecorationStyle(document);
        ReplaceCountdownEndInScripts(document, content);
        EmbedImageSources(document, template, content, imageOverrides, embedImages);
        EnsurePageEffectsStyle(document, content);
        EnsurePageEffectsScript(document, content);
        ApplyPageEffects(document, content);

        var output = document.DocumentElement.OuterHtml;
        if (includeDebugStamp)
        {
            var stamp = startedAt.ToString("HH:mm:ss.fff");
            output = $"<!-- {stamp} -->" + output;
        }

        Debug.WriteLine("[PreviewService] GenerateHtmlAsync end");
        Console.WriteLine("[PreviewService] GenerateHtmlAsync end");
        return output;
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

        private static void EnsureTypographyScaleStyle(IDocument document, ContentModel content)
        {
                var head = document.Head;
                if (head is null)
                {
                        return;
                }

                var body = document.Body;
                if (body is not null && !body.ClassList.Contains("lp-root"))
                {
                        body.ClassList.Add("lp-root");
                }

                if (document.QuerySelector("style[data-typography-scale='true']") is not null)
                {
                        return;
                }

                var styleTag = document.CreateElement("style");
                styleTag.SetAttribute("data-typography-scale", "true");
                styleTag.TextContent = @"
.lp-root {
    --lp-font-base: clamp(15px, 1.4vw, 18px);
    --lp-font-h1: clamp(24px, 3.2vw, 36px);
    --lp-font-h2: clamp(20px, 2.6vw, 30px);
    --lp-font-h3: clamp(18px, 2.1vw, 26px);
    --lp-font-note: clamp(12px, 1.1vw, 15px);
    --lp-line: 1.7;
}
.lp-root { font-size: var(--lp-font-base); line-height: var(--lp-line); }
.lp-root h1 { font-size: var(--lp-font-h1); line-height: 1.2; }
.lp-root h2 { font-size: var(--lp-font-h2); line-height: 1.3; }
.lp-root h3 { font-size: var(--lp-font-h3); line-height: 1.35; }
.lp-root p, .lp-root li, .lp-root dt, .lp-root dd, .lp-root .campaign__text { font-size: var(--lp-font-base); }
.lp-root small, .lp-root .note, .lp-root .campaign__small { font-size: var(--lp-font-note); }

.lp-section[data-section-type='ranking'] {
    --lp-font-ranking-th: clamp(16px, 2.2vw, 28px);
    --lp-font-ranking-td: clamp(18px, 2.6vw, 32px);
    --lp-font-ranking-num: clamp(16px, 2.4vw, 30px);
}
.lp-section[data-section-type='ranking'] .campaign__rank-box th { font-size: var(--lp-font-ranking-th); }
.lp-section[data-section-type='ranking'] .campaign__rank-box td { font-size: var(--lp-font-ranking-td); }
.lp-section[data-section-type='ranking'] .campaign__rank-box td.-number { font-size: var(--lp-font-ranking-num); }

.coupon-flow-section .coupon-howto-inner {
    --lp-font-coupon-note: clamp(13px, 1.2vw, 16px);
    --lp-font-coupon-button: clamp(18px, 2.2vw, 26px);
}
.coupon-flow-section .note { font-size: var(--lp-font-coupon-note); }
.coupon-flow-section .indent { font-size: var(--lp-font-coupon-note); }
.coupon-flow-section .c-btn.-l { font-size: var(--lp-font-coupon-button); }
";
                head.AppendChild(styleTag);
        }

    private static void EnsureFrameBaseStyle(IDocument document)
    {
        var head = document.Head;
        if (head is null)
        {
            return;
        }

        if (document.QuerySelector("style[data-frame-base='true']") is not null)
        {
            return;
        }

        var styleTag = document.CreateElement("style");
        styleTag.SetAttribute("data-frame-base", "true");
        styleTag.TextContent = @"
.lp-section { width: 100%; }
.lp-section-inner { width: 100%; display: flex; justify-content: center; }
.lp-frame { width: 100%; box-sizing: border-box; position: relative; }
.lp-frame-header { display: flex; align-items: center; justify-content: center; font-weight: 700; box-sizing: border-box; text-align: center; }
.lp-frame-header[data-empty='true'] { display: none; }
.lp-frame-title-img { max-width: 100%; max-height: 100%; object-fit: contain; }
.lp-frame-body { width: 100%; box-sizing: border-box; position: relative; }
.lp-frame-corner { position: absolute; pointer-events: none; width: 80px; height: 80px; max-width: 100%; max-height: 100%; object-fit: contain; }
.lp-section[data-section-type='ranking'] .lp-frame[data-band-type='image'] .ranking-inner-title { display: none !important; }
.lp-section[data-section-type='ranking'] .lp-frame[data-band-type='image'] .ranking-inner-title + * { margin-top: 8px !important; }
";
        head.AppendChild(styleTag);
    }

    private static void RenderCommonFrame(IDocument document, ContentModel content)
    {
        EnsureFrameBaseStyle(document);
        EnsureFrameStructure(document, content);
    }

    private static void EnsureFrameStructure(IDocument document, ContentModel content)
    {
        var groups = document.QuerySelectorAll(".section-group[data-section]").ToList();
        if (groups.Count == 0)
        {
            return;
        }

        foreach (var group in groups)
        {
            var rawKey = group.GetAttribute("data-section") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                continue;
            }

            var normalizedKey = NormalizeSectionKey(rawKey);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                normalizedKey = rawKey;
            }

            if (string.Equals(normalizedKey, "ranking", StringComparison.OrdinalIgnoreCase))
            {
                group.SetAttribute("data-section-type", "ranking");
            }

            if (IsCommonFrameExcluded(normalizedKey))
            {
                continue;
            }

            var existingSection = group.QuerySelector(".lp-section") as IElement;
            if (existingSection is not null)
            {
                if (string.Equals(normalizedKey, "ranking", StringComparison.OrdinalIgnoreCase))
                {
                    existingSection.SetAttribute("data-section-type", "ranking");
                }
                var existingHeader = existingSection.QuerySelector(".lp-frame-header") as IElement;
                if (existingHeader is not null)
                {
                    var title = ResolveSectionTitle(content, normalizedKey, rawKey, group) ?? string.Empty;
                    existingHeader.TextContent = title;
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        existingHeader.SetAttribute("data-empty", "true");
                    }
                    else
                    {
                        existingHeader.RemoveAttribute("data-empty");
                    }
                }

                ApplyFrameStyle(existingSection, content, normalizedKey);
                ApplyCornerDecorations(existingSection, content, normalizedKey);
                ApplyRankingFrameHeaderImage(existingSection, normalizedKey);
                EnsureRankingInnerTitleMarker(existingSection);
                continue;
            }

            var section = document.CreateElement("section");
            section.ClassList.Add("lp-section");
            section.SetAttribute("data-section-id", normalizedKey);
            if (string.Equals(normalizedKey, "ranking", StringComparison.OrdinalIgnoreCase))
            {
                section.SetAttribute("data-section-type", "ranking");
            }

            var inner = document.CreateElement("div");
            inner.ClassList.Add("lp-section-inner");

            var frame = document.CreateElement("div");
            frame.ClassList.Add("lp-frame");
            frame.SetAttribute("data-role", "frame");

            var header = document.CreateElement("div");
            header.ClassList.Add("lp-frame-header");
            header.SetAttribute("data-role", "frame-header");
            var headerTitle = ResolveSectionTitle(content, normalizedKey, rawKey, group) ?? string.Empty;
            header.TextContent = headerTitle;
            if (string.IsNullOrWhiteSpace(headerTitle))
            {
                header.SetAttribute("data-empty", "true");
            }

            var body = document.CreateElement("div");
            body.ClassList.Add("lp-frame-body");
            body.SetAttribute("data-role", "frame-body");

            var children = group.ChildNodes.ToList();
            foreach (var child in children)
            {
                body.AppendChild(child);
            }

            frame.AppendChild(header);
            frame.AppendChild(body);
            inner.AppendChild(frame);
            section.AppendChild(inner);
            group.AppendChild(section);

            ApplyFrameStyleToElements(frame, header, body, ResolveFrameStyle(content, normalizedKey));
            ApplyCornerDecorations(frame, ResolveFrameStyle(content, normalizedKey));
            ApplyRankingFrameHeaderImage(section, normalizedKey);
            EnsureRankingInnerTitleMarker(section);
        }
    }

    private static void ApplyRankingFrameHeaderImage(IElement section, string normalizedKey)
    {
        if (!string.Equals(NormalizeSectionKey(normalizedKey), "ranking", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var frame = section.QuerySelector(".lp-frame") as IElement;
        if (frame is not null)
        {
            frame.SetAttribute("data-band-type", "image");
        }

        var header = section.QuerySelector(".lp-frame-header") as IElement;
        if (header is null)
        {
            return;
        }

        header.RemoveAttribute("data-empty");
        header.TextContent = string.Empty;

        var existing = header.QuerySelector("img[data-role='ranking-title']") as IElement;
        if (existing is null)
        {
            existing = section.Owner?.CreateElement("img");
            if (existing is null)
            {
                return;
            }
            existing.SetAttribute("data-role", "ranking-title");
            existing.ClassList.Add("lp-frame-title-img");
            header.AppendChild(existing);
        }

        existing.SetAttribute("src", "images/title-cp04.png");
        existing.SetAttribute("alt", "ランキング");
    }

    private static void EnsureRankingInnerTitleMarker(IElement section)
    {
        if (section.QuerySelector("[data-section-type='ranking']") is null && !string.Equals(section.GetAttribute("data-section-type"), "ranking", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var title = section.QuerySelector(".rank-ref .campaign__rank-title") as IElement;
        if (title is null)
        {
            return;
        }

        title.ClassList.Add("ranking-inner-title");
        title.SetAttribute("data-inner-title", "true");
    }

    private static void EnsureSectionBodyOnly(IDocument document, ContentModel content)
    {
        foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
        {
            var rawKey = group.GetAttribute("data-section") ?? string.Empty;
            var normalizedKey = NormalizeSectionKey(rawKey);
            if (IsCommonFrameExcluded(normalizedKey))
            {
                continue;
            }

            var blocks = group.QuerySelectorAll(".campaign__block").ToList();
            var boxes = group.QuerySelectorAll(".campaign__box").ToList();
            if (blocks.Count == 0 && boxes.Count == 0)
            {
                continue;
            }

            var nodes = new List<INode>();

            if (blocks.Count > 0)
            {
                foreach (var block in blocks)
                {
                    var box = block.QuerySelector(".campaign__box") as IElement;
                    if (box is not null)
                    {
                        foreach (var heading in box.QuerySelectorAll(".campaign__heading").ToList())
                        {
                            heading.Remove();
                        }

                        var bodyRoot = box.QuerySelector(".campaign__inner") ?? box;
                        nodes.AddRange(bodyRoot.ChildNodes.ToList());

                        foreach (var sibling in block.ChildNodes.ToList())
                        {
                            if (!ReferenceEquals(sibling, box))
                            {
                                nodes.Add(sibling);
                            }
                        }
                    }
                    else
                    {
                        nodes.AddRange(block.ChildNodes.ToList());
                    }
                }
            }
            else
            {
                foreach (var box in boxes)
                {
                    foreach (var heading in box.QuerySelectorAll(".campaign__heading").ToList())
                    {
                        heading.Remove();
                    }

                    var bodyRoot = box.QuerySelector(".campaign__inner") ?? box;
                    nodes.AddRange(bodyRoot.ChildNodes.ToList());
                }
            }

            if (nodes.Count == 0)
            {
                continue;
            }

            group.InnerHtml = string.Empty;
            foreach (var node in nodes)
            {
                group.AppendChild(node);
            }
        }
    }

    private static bool IsCommonFrameExcluded(string? normalizedKey)
    {
        var key = NormalizeSectionKey(normalizedKey);
        return string.Equals(key, "countdown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "conditionscontactbanners", StringComparison.OrdinalIgnoreCase);
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
.lp-highlight-design { outline-color: rgba(59,130,246,0.9); box-shadow: 0 0 0 4px rgba(59,130,246,0.25); }
    .lp-editing { outline: 2px dashed rgba(14,116,144,0.9); outline-offset: 2px; box-shadow: 0 0 0 4px rgba(14,116,144,0.2); }
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

        if (IsPresetBackground(setting))
        {
            rules.AppendLine("html, body, .lp-canvas, .page, .lp-wrapper, .l-wrapper { background: transparent !important; }");
        }

        if (background.TransparentSections)
        {
            rules.AppendLine(".lp-canvas .section-group, .lp-canvas section, .lp-canvas .campaign__block, .lp-canvas .campaign__box, .lp-canvas .campaign__subBox, .lp-canvas .ranking-section, .lp-canvas .store-search-section .store-search-body { background: transparent !important; }");
        }

        styleTag.TextContent = rules.ToString();

        if (IsPresetBackground(setting))
        {
            EnsurePresetBackgroundStyle(document);
            EnsurePresetBackgroundLayer(document, setting.Preset);
        }
        else
        {
            RemovePresetBackgroundLayer(document);
        }

        ApplyPageBackgroundMediaLayers(document, wrapper, setting);
    }

        private static bool IsPresetBackground(BackgroundSetting setting)
        {
                var mode = BackgroundRenderService.ResolveSourceType(setting);
                return mode == "preset" && !string.IsNullOrWhiteSpace(setting.Preset?.CssClass);
        }

        private static void EnsurePresetBackgroundLayer(IDocument document, BackgroundPresetSelection? preset)
        {
            if (preset is null)
            {
                return;
            }

            var wrapper = document.QuerySelector(".lp-canvas") as IElement ?? document.Body;
            if (wrapper is null)
            {
                return;
            }

            var layer = document.QuerySelector(".lp-bg") as IElement;
            if (layer is null)
            {
                layer = document.CreateElement("div");
                layer.ClassList.Add("lp-bg");
                wrapper.Prepend(layer);
            }

                foreach (var cls in layer.ClassList.Where(name => name.StartsWith("bg--", StringComparison.OrdinalIgnoreCase)).ToList())
                {
                        layer.ClassList.Remove(cls);
                }

                if (!string.IsNullOrWhiteSpace(preset.CssClass))
                {
                        layer.ClassList.Add(preset.CssClass);
                }

                var baseColor = string.IsNullOrWhiteSpace(preset.ColorA) ? "#f8fafc" : preset.ColorA;
                var accentColor = string.IsNullOrWhiteSpace(preset.ColorB) ? "#cbd5f5" : preset.ColorB;
                var opacity = Math.Clamp(preset.Opacity ?? 1, 0, 1).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                var scale = Math.Clamp(preset.Scale ?? 1, 0.5, 2).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                SetCssCustomProperty(layer, "--bg-base", baseColor);
                SetCssCustomProperty(layer, "--bg-accent", accentColor);
                SetCssCustomProperty(layer, "--bg-opacity", opacity);
                SetCssCustomProperty(layer, "--bg-scale", scale);
        }

        private static void RemovePresetBackgroundLayer(IDocument document)
        {
                document.QuerySelector(".lp-bg")?.Remove();
        }

        private static void EnsurePresetBackgroundStyle(IDocument document)
        {
                var head = document.Head;
                if (head is null || document.QuerySelector("style[data-lp-bg-presets='true']") is not null)
                {
                        return;
                }

                var styleTag = document.CreateElement("style");
                styleTag.SetAttribute("data-lp-bg-presets", "true");
                styleTag.TextContent = @"
.lp-canvas { position: relative; min-height: 100vh; }
.lp-canvas > * { position: relative; z-index: 1; }
.lp-bg {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    z-index: 0;
    pointer-events: none;
    background-color: var(--bg-base, #f8fafc);
    background-repeat: repeat;
    opacity: var(--bg-opacity, 1);
}

.lp-bg.bg--paper-grid-1 {
    background-image:
        linear-gradient(var(--bg-accent) 1px, transparent 1px),
        linear-gradient(90deg, var(--bg-accent) 1px, transparent 1px);
    background-size: calc(24px * var(--bg-scale, 1)) calc(24px * var(--bg-scale, 1));
}

.lp-bg.bg--paper-grid-2 {
    background-image:
        linear-gradient(var(--bg-accent) 2px, transparent 2px),
        linear-gradient(90deg, var(--bg-accent) 2px, transparent 2px);
    background-size: calc(32px * var(--bg-scale, 1)) calc(32px * var(--bg-scale, 1));
}

.lp-bg.bg--paper-cross {
    background-image:
        radial-gradient(circle, var(--bg-accent) 1.5px, transparent 1.6px),
        radial-gradient(circle, var(--bg-accent) 1.5px, transparent 1.6px);
    background-size: calc(20px * var(--bg-scale, 1)) calc(20px * var(--bg-scale, 1));
    background-position: 0 0, calc(10px * var(--bg-scale, 1)) calc(10px * var(--bg-scale, 1));
}

.lp-bg.bg--dot-soft {
    background-image: radial-gradient(circle, var(--bg-accent) 1.5px, transparent 1.6px);
    background-size: calc(22px * var(--bg-scale, 1)) calc(22px * var(--bg-scale, 1));
}

.lp-bg.bg--dot-polka {
    background-image: radial-gradient(circle, var(--bg-accent) 3px, transparent 3.2px);
    background-size: calc(26px * var(--bg-scale, 1)) calc(26px * var(--bg-scale, 1));
}

.lp-bg.bg--stripe-thin {
    background-image: repeating-linear-gradient(
        0deg,
        var(--bg-accent) 0 1px,
        transparent 1px 8px
    );
    background-size: 100% calc(10px * var(--bg-scale, 1));
}

.lp-bg.bg--stripe-bold {
    background-image: repeating-linear-gradient(
        0deg,
        var(--bg-accent) 0 10px,
        transparent 10px 22px
    );
    background-size: 100% calc(24px * var(--bg-scale, 1));
}

.lp-bg.bg--diagonal-1 {
    background-image: repeating-linear-gradient(
        45deg,
        var(--bg-accent) 0 1px,
        transparent 1px 10px
    );
    background-size: calc(20px * var(--bg-scale, 1)) calc(20px * var(--bg-scale, 1));
}

.lp-bg.bg--diagonal-2 {
    background-image: repeating-linear-gradient(
        135deg,
        var(--bg-accent) 0 2px,
        transparent 2px 14px
    );
    background-size: calc(24px * var(--bg-scale, 1)) calc(24px * var(--bg-scale, 1));
}

.lp-bg.bg--diagonal-3 {
    background-image: repeating-linear-gradient(
        60deg,
        var(--bg-accent) 0 3px,
        transparent 3px 18px
    );
    background-size: calc(28px * var(--bg-scale, 1)) calc(28px * var(--bg-scale, 1));
}

.lp-bg.bg--zigzag {
    background-image:
        linear-gradient(135deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(225deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(45deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(315deg, var(--bg-accent) 25%, transparent 25%);
    background-position: 0 0, 0 0, calc(12px * var(--bg-scale, 1)) calc(12px * var(--bg-scale, 1)), calc(12px * var(--bg-scale, 1)) calc(12px * var(--bg-scale, 1));
    background-size: calc(24px * var(--bg-scale, 1)) calc(24px * var(--bg-scale, 1));
}

.lp-bg.bg--zigzag-3d {
    background-image:
        linear-gradient(135deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(225deg, var(--bg-accent) 25%, transparent 25%);
    background-size: calc(28px * var(--bg-scale, 1)) calc(28px * var(--bg-scale, 1));
    background-position: 0 0, calc(14px * var(--bg-scale, 1)) calc(14px * var(--bg-scale, 1));
}

.lp-bg.bg--rhombus {
    background-image:
        linear-gradient(45deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(-45deg, var(--bg-accent) 25%, transparent 25%);
    background-size: calc(26px * var(--bg-scale, 1)) calc(26px * var(--bg-scale, 1));
    background-position: 0 0, calc(13px * var(--bg-scale, 1)) calc(13px * var(--bg-scale, 1));
}

.lp-bg.bg--triangle {
    background-image:
        linear-gradient(60deg, var(--bg-accent) 25%, transparent 25%),
        linear-gradient(-60deg, var(--bg-accent) 25%, transparent 25%);
    background-size: calc(28px * var(--bg-scale, 1)) calc(28px * var(--bg-scale, 1));
    background-position: 0 0, calc(14px * var(--bg-scale, 1)) calc(14px * var(--bg-scale, 1));
}

.lp-bg.bg--isometric {
    background-image:
        linear-gradient(30deg, var(--bg-accent) 12%, transparent 12%),
        linear-gradient(150deg, var(--bg-accent) 12%, transparent 12%),
        linear-gradient(90deg, var(--bg-accent) 2px, transparent 2px);
    background-size: calc(36px * var(--bg-scale, 1)) calc(36px * var(--bg-scale, 1));
    background-position: 0 0, 0 0, 0 0;
}

.lp-bg.bg--wave {
    background-image:
        radial-gradient(circle at 0 50%, var(--bg-accent) 2px, transparent 2px),
        radial-gradient(circle at 50% 50%, var(--bg-accent) 2px, transparent 2px);
    background-size: calc(26px * var(--bg-scale, 1)) calc(20px * var(--bg-scale, 1));
}

.lp-bg.bg--moon {
    background-image:
        radial-gradient(circle at 30% 30%, var(--bg-accent) 6px, transparent 6px),
        radial-gradient(circle at 70% 70%, var(--bg-accent) 6px, transparent 6px);
    background-size: calc(40px * var(--bg-scale, 1)) calc(40px * var(--bg-scale, 1));
}

.lp-bg.bg--circle {
    background-image: radial-gradient(circle, transparent 6px, var(--bg-accent) 6px 7px, transparent 7px);
    background-size: calc(28px * var(--bg-scale, 1)) calc(28px * var(--bg-scale, 1));
}

.lp-bg.bg--box {
    background-image:
        linear-gradient(var(--bg-accent) 1px, transparent 1px),
        linear-gradient(90deg, var(--bg-accent) 1px, transparent 1px);
    background-size: calc(34px * var(--bg-scale, 1)) calc(34px * var(--bg-scale, 1));
    background-position: calc(4px * var(--bg-scale, 1)) calc(4px * var(--bg-scale, 1));
}

.lp-bg.bg--cross {
    background-image:
        linear-gradient(90deg, transparent 46%, var(--bg-accent) 46% 54%, transparent 54%),
        linear-gradient(transparent 46%, var(--bg-accent) 46% 54%, transparent 54%);
    background-size: calc(24px * var(--bg-scale, 1)) calc(24px * var(--bg-scale, 1));
}
";
                head.AppendChild(styleTag);
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

            if (string.Equals(NormalizeSectionKey(key), "countdown", StringComparison.OrdinalIgnoreCase))
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

            private static void ApplySectionStyles(IDocument document, ContentModel content, string? editingSectionKey)
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
                var needsFadeIn = false;

                var normalizedEditingKey = NormalizeSectionKey(editingSectionKey);
                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var key = group.GetAttribute("data-section");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    var normalizedKey = NormalizeSectionKey(key);
                    if (!string.IsNullOrWhiteSpace(normalizedKey))
                    {
                        group.SetAttribute("data-section-id", normalizedKey);
                        group.SetAttribute("data-section-key", normalizedKey);
                    }

                    var section = group.Children.FirstOrDefault(child => string.Equals(child.TagName, "SECTION", StringComparison.OrdinalIgnoreCase));
                    if (section is not null && !string.IsNullOrWhiteSpace(normalizedKey))
                    {
                        section.SetAttribute("data-section-id", normalizedKey);
                    }

                    group.ClassList.Add("lp-section-frame");
                    if (section is not null)
                    {
                        section.ClassList.Add("lp-section-frame");
                    }

                    var isEditing = !string.IsNullOrWhiteSpace(normalizedEditingKey)
                        && string.Equals(normalizedKey, normalizedEditingKey, StringComparison.OrdinalIgnoreCase);
                    if (isEditing)
                    {
                        group.ClassList.Add("lp-editing");
                        group.SetAttribute("data-editing", "true");
                        if (section is not null)
                        {
                            section.ClassList.Add("lp-editing");
                        }
                    }
                    else
                    {
                        group.ClassList.Remove("lp-editing");
                        group.RemoveAttribute("data-editing");
                        if (section is not null)
                        {
                            section.ClassList.Remove("lp-editing");
                        }
                    }

                    if (!IsEditorManagedSectionKey(key, content))
                    {
                        continue;
                    }

                    if (string.Equals(normalizedKey, "countdown", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var style = ResolveSectionStyle(content, key);
                    if (style is null)
                    {
                        continue;
                    }

                    var selector = string.IsNullOrWhiteSpace(normalizedKey)
                        ? $".lp-canvas .section-group[data-section='{key}']"
                        : $".lp-canvas .section-group[data-section-id='{normalizedKey}']";
                    var rule = BuildSectionStyleRule(style, selector);
                    if (!string.IsNullOrWhiteSpace(rule))
                    {
                        rules.AppendLine(rule);
                    }

                    var overlayRule = BuildSectionDesignOverlayRule(style, selector);
                    if (!string.IsNullOrWhiteSpace(overlayRule))
                    {
                        rules.AppendLine(overlayRule);
                        rules.AppendLine(BuildSectionDesignContentRule(selector));
                    }

                    if (style.Design is not null
                        && string.Equals(style.Design.Animation, "fadein", StringComparison.OrdinalIgnoreCase))
                    {
                        needsFadeIn = true;
                    }

                    var inlineStyle = BuildSectionStyleInline(style);
                    if (!string.IsNullOrWhiteSpace(inlineStyle))
                    {
                        AppendInlineStyle(group, inlineStyle);
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

                if (needsFadeIn)
                {
                    rules.AppendLine("@keyframes lp-section-fadein { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }");
                }

                styleTag.TextContent = rules.ToString();
            }

            private static string BuildSectionStyleInline(SectionStyleModel style)
            {
                var rules = new List<string>();
                var design = style.Design;
                var hasDesign = design is not null;
                if (hasDesign)
                {
                    var background = BuildDesignBackground(design!);
                    if (!string.IsNullOrWhiteSpace(background))
                    {
                        rules.Add($"background: {background}");
                    }
                    if (string.Equals(design!.Type, "image", StringComparison.OrdinalIgnoreCase))
                    {
                        rules.Add("background-size: cover");
                        rules.Add("background-repeat: no-repeat");
                        rules.Add("background-position: center");
                    }
                    if (design.BorderRadius is > 0)
                    {
                        rules.Add($"border-radius: {design.BorderRadius}px");
                    }
                    var (paddingX, paddingY) = ResolveDesignPadding(design);
                    if (paddingY is > 0)
                    {
                        rules.Add($"padding-top: {paddingY}px");
                        rules.Add($"padding-bottom: {paddingY}px");
                    }
                    if (paddingX is > 0)
                    {
                        rules.Add($"padding-left: {paddingX}px");
                        rules.Add($"padding-right: {paddingX}px");
                    }
                    if (design.MarginBottom is >= 0)
                    {
                        rules.Add($"margin-bottom: {design.MarginBottom}px");
                    }
                    if (design.BorderWidth is > 0)
                    {
                        var borderColor = SanitizeCssColor(design.BorderColor) ?? "#e5e7eb";
                        var borderWidth = design.BorderWidth.Value;
                        rules.Add($"border: {borderWidth}px solid {borderColor}");
                    }
                    var shadow = ResolveSectionShadow(design.ShadowLevel ?? "off");
                    if (design.AccentLineEnabled)
                    {
                        var accentColor = SanitizeCssColor(design.AccentColor) ?? "#4f46e5";
                        var accentHeight = design.AccentHeight is > 0 ? design.AccentHeight.Value : 4;
                        var accentShadow = $"inset 0 {accentHeight}px 0 0 {accentColor}";
                        shadow = string.IsNullOrWhiteSpace(shadow) ? accentShadow : $"{shadow}, {accentShadow}";
                    }
                    if (!string.IsNullOrWhiteSpace(shadow))
                    {
                        rules.Add($"box-shadow: {shadow}");
                    }
                    if (string.Equals(design.Animation, "fadein", StringComparison.OrdinalIgnoreCase))
                    {
                        rules.Add("animation: lp-section-fadein 0.6s ease");
                    }
                    if (RequiresOverlay(design))
                    {
                        rules.Add("position: relative");
                        rules.Add("overflow: hidden");
                    }
                }
                else
                {
                    var borderColor = SanitizeCssColor(style.BorderColor);
                    if (!string.IsNullOrWhiteSpace(borderColor))
                    {
                        var borderWidth = style.BorderWidth is > 0 ? style.BorderWidth.Value : 1;
                        var borderStyle = string.IsNullOrWhiteSpace(style.BorderStyle) ? "solid" : style.BorderStyle;
                        rules.Add($"border: {borderWidth}px {borderStyle} {borderColor}");
                    }
                    if (style.Radius is > 0)
                    {
                        rules.Add($"border-radius: {style.Radius}px");
                    }
                    var shadow = ResolveSectionShadow(style.Shadow);
                    if (!string.IsNullOrWhiteSpace(shadow))
                    {
                        rules.Add($"box-shadow: {shadow}");
                    }
                    if (style.PaddingTop is > 0)
                    {
                        rules.Add($"padding-top: {style.PaddingTop}px");
                    }
                    if (style.PaddingRight is > 0)
                    {
                        rules.Add($"padding-right: {style.PaddingRight}px");
                    }
                    if (style.PaddingBottom is > 0)
                    {
                        rules.Add($"padding-bottom: {style.PaddingBottom}px");
                    }
                    if (style.PaddingLeft is > 0)
                    {
                        rules.Add($"padding-left: {style.PaddingLeft}px");
                    }
                }

                return rules.Count == 0 ? string.Empty : string.Join("; ", rules);
            }

            private static void AppendInlineStyle(IElement element, string style)
            {
                if (element is null || string.IsNullOrWhiteSpace(style))
                {
                    return;
                }

                var existing = element.GetAttribute("style");
                if (string.IsNullOrWhiteSpace(existing))
                {
                    element.SetAttribute("style", style);
                    return;
                }

                element.SetAttribute("style", existing.Trim().TrimEnd(';') + "; " + style);
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

                var design = style.Design;
                var hasDesign = design is not null;
                var overlayNeeded = design is not null && RequiresOverlay(design);

                if (hasDesign)
                {
                    var background = BuildDesignBackground(design!);
                    if (!string.IsNullOrWhiteSpace(background))
                    {
                        rules.Add($"background: {background} !important;");
                    }
                    if (string.Equals(design!.Type, "image", StringComparison.OrdinalIgnoreCase))
                    {
                        rules.Add("background-size: cover !important;");
                        rules.Add("background-repeat: no-repeat !important;");
                        rules.Add("background-position: center !important;");
                    }

                    if (design.BorderRadius is > 0)
                    {
                        rules.Add($"border-radius: {design.BorderRadius}px !important;");
                    }

                    var (paddingX, paddingY) = ResolveDesignPadding(design);
                    if (paddingY is > 0)
                    {
                        rules.Add($"padding-top: {paddingY}px !important;");
                        rules.Add($"padding-bottom: {paddingY}px !important;");
                    }
                    if (paddingX is > 0)
                    {
                        rules.Add($"padding-left: {paddingX}px !important;");
                        rules.Add($"padding-right: {paddingX}px !important;");
                    }

                    if (design.MarginBottom is >= 0)
                    {
                        rules.Add($"margin-bottom: {design.MarginBottom}px !important;");
                    }

                    if (design.BorderWidth is > 0)
                    {
                        var borderColor = SanitizeCssColor(design.BorderColor) ?? "#e5e7eb";
                        var borderWidth = design.BorderWidth.Value;
                        rules.Add($"border: {borderWidth}px solid {borderColor} !important;");
                    }

                    var shadow = ResolveSectionShadow(design.ShadowLevel ?? "off");
                    if (design.AccentLineEnabled)
                    {
                        var accentColor = SanitizeCssColor(design.AccentColor) ?? "#4f46e5";
                        var accentHeight = design.AccentHeight is > 0 ? design.AccentHeight.Value : 4;
                        var accentShadow = $"inset 0 {accentHeight}px 0 0 {accentColor}";
                        shadow = string.IsNullOrWhiteSpace(shadow) ? accentShadow : $"{shadow}, {accentShadow}";
                    }
                    if (!string.IsNullOrWhiteSpace(shadow))
                    {
                        rules.Add($"box-shadow: {shadow} !important;");
                    }

                    if (string.Equals(design.Animation, "fadein", StringComparison.OrdinalIgnoreCase))
                    {
                        rules.Add("animation: lp-section-fadein 0.6s ease;");
                    }

                    if (overlayNeeded)
                    {
                        rules.Add("position: relative !important;");
                        rules.Add("overflow: hidden !important;");
                    }
                }
                else
                {
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
                }

                if (rules.Count == 0)
                {
                    return string.Empty;
                }

                return $"{selector} {{ {string.Join(" ", rules)} }}";
            }

            private static (int? PaddingX, int? PaddingY) ResolveDesignPadding(SectionDesignModel design)
            {
                if (string.Equals(design.PaddingPreset, "custom", StringComparison.OrdinalIgnoreCase))
                {
                    return (design.PaddingX, design.PaddingY);
                }

                return design.PaddingPreset switch
                {
                    "sm" => (16, 16),
                    "lg" => (32, 28),
                    _ => (24, 24)
                };
            }

            private static string BuildDesignBackground(SectionDesignModel design)
            {
                var type = design.Type?.ToLowerInvariant() ?? "simple";
                if (type == "gradient")
                {
                    var a = SanitizeCssColor(design.GradientColorA) ?? "#e0f2fe";
                    var b = SanitizeCssColor(design.GradientColorB) ?? "#ffffff";
                    var deg = design.GradientDirection is >= 0 ? design.GradientDirection.Value : 135;
                    return $"linear-gradient({deg}deg, {a}, {b})";
                }

                if (type == "image")
                {
                    var url = SanitizeCssUrl(design.ImageUrl);
                    return string.IsNullOrWhiteSpace(url) ? string.Empty : $"url('{url}')";
                }

                var color = SanitizeCssColor(design.BackgroundColor) ?? "#ffffff";
                var opacity = design.BackgroundOpacity is >= 0 and <= 1 ? design.BackgroundOpacity.Value : 1;
                return ToRgba(color, opacity);
            }

            private static bool RequiresOverlay(SectionDesignModel design)
            {
                if (string.Equals(design.Type, "image", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return !string.Equals(design.PatternType, "off", StringComparison.OrdinalIgnoreCase);
            }

            private static string BuildSectionDesignOverlayRule(SectionStyleModel style, string selector)
            {
                var design = style.Design;
                if (design is null)
                {
                    return string.Empty;
                }

                var pattern = design.PatternType?.ToLowerInvariant() ?? "off";
                if (!string.Equals(design.Type, "image", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(pattern, "off", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                var overlayColor = SanitizeCssColor(design.OverlayColor);
                var overlayOpacity = design.OverlayOpacity is >= 0 and <= 1 ? design.OverlayOpacity.Value : 0.0;
                var baseOverlay = string.Equals(design.Type, "image", StringComparison.OrdinalIgnoreCase)
                    ? (!string.IsNullOrWhiteSpace(overlayColor) ? ToRgba(overlayColor!, overlayOpacity) : "transparent")
                    : "transparent";

                var patternLayer = pattern switch
                {
                    "dots" => "radial-gradient(circle at 2px 2px, rgba(255,255,255,0.45) 1.2px, transparent 1.2px)",
                    "stripes" => "linear-gradient(135deg, rgba(255,255,255,0.45) 0, rgba(255,255,255,0.45) 10px, transparent 10px, transparent 20px)",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(baseOverlay) && string.IsNullOrWhiteSpace(patternLayer))
                {
                    return string.Empty;
                }

                var layers = string.IsNullOrWhiteSpace(patternLayer)
                    ? baseOverlay
                    : $"{baseOverlay}, {patternLayer}";
                var size = pattern switch
                {
                    "dots" => "8px 8px",
                    "stripes" => "24px 24px",
                    _ => "auto"
                };

                return $"{selector}::after {{ content: ''; position: absolute; inset: 0; background: {layers}; background-size: {size}; pointer-events: none; border-radius: inherit; z-index: 0; }}";
            }

            private static string BuildSectionDesignContentRule(string selector)
            {
                return $"{selector} > * {{ position: relative; z-index: 1; }}";
            }

            private static string? ResolveSectionShadow(string value)
            {
                var normalized = value?.Trim().ToLowerInvariant();
                return normalized switch
                {
                    "sm" => "0 4px 10px rgba(15, 23, 42, 0.12)",
                    "md" => "0 10px 24px rgba(15, 23, 42, 0.18)",
                    "lg" => "0 18px 38px rgba(15, 23, 42, 0.22)",
                    "none" or "off" or "" or null => null,
                    _ => value
                };
            }

            private static void ApplyFrameStyle(IElement section, ContentModel content, string normalizedKey)
            {
                var frame = section.QuerySelector(".lp-frame") as IElement;
                var header = section.QuerySelector(".lp-frame-header") as IElement;
                var body = section.QuerySelector(".lp-frame-body") as IElement;
                if (frame is null || header is null || body is null)
                {
                    return;
                }

                ApplyFrameStyleToElements(frame, header, body, ResolveFrameStyle(content, normalizedKey));
            }

            private static FrameStyle ResolveFrameStyle(ContentModel content, string normalizedKey)
            {
                return content.FrameDefaultStyle ?? content.CardThemeStyle ?? new FrameStyle();
            }

            private static void ApplyFrameAnimations(IDocument document, ContentModel content)
            {
                var groups = document.QuerySelectorAll(".section-group[data-section]").ToList();
                if (groups.Count == 0)
                {
                    return;
                }

                foreach (var group in groups)
                {
                    var rawKey = group.GetAttribute("data-section") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(rawKey))
                    {
                        continue;
                    }

                    var normalizedKey = NormalizeSectionKey(rawKey);
                    if (string.IsNullOrWhiteSpace(normalizedKey))
                    {
                        normalizedKey = rawKey;
                    }

                    var section = group.QuerySelector(".lp-section") as IElement;
                    if (section is null)
                    {
                        continue;
                    }

                    if (!content.AnimationPreviewEnabled)
                    {
                        ClearFrameAnimationTargets(section);
                        continue;
                    }

                    var overrideStyle = FindFrameStyleOverrideByNormalizedKey(content, normalizedKey);
                    var baseStyle = ResolveFrameStyle(content, normalizedKey);
                    var targets = (overrideStyle?.AnimationTargets?.Count ?? 0) > 0
                        ? overrideStyle!.AnimationTargets
                        : baseStyle.AnimationTargets;
                    ClearFrameAnimationTargets(section);

                    if (targets is null || targets.Count == 0)
                    {
                        continue;
                    }

                    foreach (var pair in targets)
                    {
                        ApplyFrameAnimationToTargets(section, pair.Key, pair.Value);
                    }
                }
            }

            private static void ClearFrameAnimationTargets(IElement section)
            {
                foreach (var target in GetFrameAnimationElements(section, "outer"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "band"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "inner"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "content"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "corner-tl"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "corner-tr"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "corner-bl"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "corner-br"))
                {
                    ClearFrameAnimation(target);
                }
                foreach (var target in GetFrameAnimationElements(section, "tab"))
                {
                    ClearFrameAnimation(target);
                }
            }

            private static void ApplyFrameAnimationToTargets(IElement section, string targetKey, FrameAnimationTargetSetting setting)
            {
                var elements = GetFrameAnimationElements(section, targetKey);
                foreach (var element in elements)
                {
                    ApplyFrameAnimationToElement(element, setting);
                }
            }

            private static IEnumerable<IElement> GetFrameAnimationElements(IElement section, string targetKey)
            {
                if (section is null)
                {
                    return Enumerable.Empty<IElement>();
                }

                var key = (targetKey ?? string.Empty).Trim().ToLowerInvariant();
                return key switch
                {
                    "outer" => section.QuerySelectorAll(".lp-frame").OfType<IElement>().ToList(),
                    "band" => section.QuerySelectorAll(".lp-frame-header").OfType<IElement>().ToList(),
                    "inner" => section.QuerySelectorAll(".lp-frame-body").OfType<IElement>().ToList(),
                    "content" => section.QuerySelectorAll(".lp-frame-body > *").OfType<IElement>().ToList(),
                    "corner-tl" => section.QuerySelectorAll(".lp-frame-corner[data-corner='tl']").OfType<IElement>().ToList(),
                    "corner-tr" => section.QuerySelectorAll(".lp-frame-corner[data-corner='tr']").OfType<IElement>().ToList(),
                    "corner-bl" => section.QuerySelectorAll(".lp-frame-corner[data-corner='bl']").OfType<IElement>().ToList(),
                    "corner-br" => section.QuerySelectorAll(".lp-frame-corner[data-corner='br']").OfType<IElement>().ToList(),
                    "tab" => section.QuerySelectorAll(".sticky-tabs__tab").OfType<IElement>().ToList(),
                    _ => Enumerable.Empty<IElement>()
                };
            }

            private static void ApplyFrameAnimationToElement(IElement element, FrameAnimationTargetSetting setting)
            {
                if (element is null)
                {
                    return;
                }

                ClearFrameAnimation(element);

                if (setting is null || !setting.Enabled)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(setting.PresetId)
                    || string.Equals(setting.PresetId, "none", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                element.SetAttribute("data-frame-anim", setting.PresetId);
                element.SetAttribute("data-frame-anim-trigger", string.IsNullOrWhiteSpace(setting.Trigger) ? "scroll" : setting.Trigger);
                if (setting.DurationMs is > 0)
                {
                    element.SetAttribute("data-frame-anim-duration", setting.DurationMs.Value.ToString());
                }
                if (setting.DelayMs is >= 0)
                {
                    element.SetAttribute("data-frame-anim-delay", setting.DelayMs.Value.ToString());
                }
                if (!string.IsNullOrWhiteSpace(setting.Easing))
                {
                    element.SetAttribute("data-frame-anim-easing", setting.Easing);
                }
                element.SetAttribute("data-frame-anim-loop", setting.Loop ? "true" : "false");
                element.ClassList.Add("lp-frame-anim");
                element.ClassList.Add($"anim-{setting.PresetId}");
            }

            private static void ClearFrameAnimation(IElement element)
            {
                element.RemoveAttribute("data-frame-anim");
                element.RemoveAttribute("data-frame-anim-trigger");
                element.RemoveAttribute("data-frame-anim-duration");
                element.RemoveAttribute("data-frame-anim-delay");
                element.RemoveAttribute("data-frame-anim-easing");
                element.RemoveAttribute("data-frame-anim-loop");
                element.ClassList.Remove("lp-frame-anim");
                element.ClassList.Remove("is-inview");

                foreach (var cls in element.ClassList.ToList())
                {
                    if (cls.StartsWith("anim-", StringComparison.OrdinalIgnoreCase))
                    {
                        element.ClassList.Remove(cls);
                    }
                }
            }

            private static void EnsureFrameAnimationStyle(IDocument document)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("style[data-frame-anim-style='true']") is not null)
                {
                    return;
                }

                var style = document.CreateElement("style");
                style.SetAttribute("data-frame-anim-style", "true");
                style.TextContent = @"
.lp-frame-anim {
  opacity: 0;
  animation-duration: var(--lp-frame-anim-duration, 600ms);
  animation-delay: var(--lp-frame-anim-delay, 0ms);
  animation-timing-function: var(--lp-frame-anim-easing, ease);
  animation-fill-mode: both;
  animation-iteration-count: var(--lp-frame-anim-iterations, 1);
  animation-play-state: paused;
  will-change: transform, opacity, filter;
}
.lp-frame-anim.is-inview { opacity: 1; animation-play-state: running; }
.lp-frame-anim[data-frame-anim-trigger='load'] { opacity: 1; animation-play-state: running; }
.lp-frame-anim[data-frame-anim-trigger='hover'] { opacity: 1; animation-play-state: paused; }
.lp-frame-anim[data-frame-anim-trigger='hover']:hover { animation-play-state: running; }

.anim-fade-in { animation-name: anim-fade-in; }
.anim-fade-up { animation-name: anim-fade-up; }
.anim-fade-down { animation-name: anim-fade-down; }
.anim-fade-left { animation-name: anim-fade-left; }
.anim-fade-right { animation-name: anim-fade-right; }
.anim-slide-up { animation-name: anim-slide-up; }
.anim-slide-left { animation-name: anim-slide-left; }
.anim-slide-right { animation-name: anim-slide-right; }
.anim-zoom-in { animation-name: anim-zoom-in; }
.anim-blur-in { animation-name: anim-blur-in; }
.anim-bounce-in { animation-name: anim-bounce-in; }
.anim-pop { animation-name: anim-pop; }
.anim-rubber { animation-name: anim-rubber; }
.anim-swing { animation-name: anim-swing; }
.anim-tada { animation-name: anim-tada; }
.anim-flip { animation-name: anim-flip; }
.anim-rotate-in { animation-name: anim-rotate-in; }
.anim-skew { animation-name: anim-skew; }
.anim-glow { animation-name: anim-glow; }
.anim-shimmer { animation-name: anim-shimmer; }
.anim-float { animation-name: anim-float; }
.anim-breathe { animation-name: anim-breathe; }
.anim-gentle-wiggle { animation-name: anim-gentle-wiggle; }

@keyframes anim-fade-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes anim-fade-up { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
@keyframes anim-fade-down { from { opacity: 0; transform: translateY(-20px); } to { opacity: 1; transform: translateY(0); } }
@keyframes anim-fade-left { from { opacity: 0; transform: translateX(20px); } to { opacity: 1; transform: translateX(0); } }
@keyframes anim-fade-right { from { opacity: 0; transform: translateX(-20px); } to { opacity: 1; transform: translateX(0); } }
@keyframes anim-slide-up { from { transform: translateY(26px); } to { transform: translateY(0); } }
@keyframes anim-slide-left { from { transform: translateX(26px); } to { transform: translateX(0); } }
@keyframes anim-slide-right { from { transform: translateX(-26px); } to { transform: translateX(0); } }
@keyframes anim-zoom-in { from { transform: scale(0.92); opacity: 0; } to { transform: scale(1); opacity: 1; } }
@keyframes anim-blur-in { from { opacity: 0; filter: blur(8px); } to { opacity: 1; filter: blur(0); } }
@keyframes anim-bounce-in { 0% { transform: scale(0.9); opacity: 0; } 60% { transform: scale(1.05); opacity: 1; } 100% { transform: scale(1); } }
@keyframes anim-pop { 0% { transform: scale(0.85); opacity: 0; } 100% { transform: scale(1); opacity: 1; } }
@keyframes anim-rubber { 0% { transform: scale3d(1,1,1); } 30% { transform: scale3d(1.15,0.85,1); } 40% { transform: scale3d(0.9,1.1,1); } 50% { transform: scale3d(1.05,0.95,1); } 65% { transform: scale3d(0.98,1.02,1); } 100% { transform: scale3d(1,1,1); } }
@keyframes anim-swing { 20% { transform: rotate(6deg); } 40% { transform: rotate(-4deg); } 60% { transform: rotate(2deg); } 80% { transform: rotate(-2deg); } 100% { transform: rotate(0); } }
@keyframes anim-tada { 0% { transform: scale3d(1,1,1); } 10%, 20% { transform: scale3d(0.9,0.9,0.9) rotate(-3deg); } 30%, 50%, 70%, 90% { transform: scale3d(1.1,1.1,1.1) rotate(3deg); } 40%, 60%, 80% { transform: scale3d(1.1,1.1,1.1) rotate(-3deg); } 100% { transform: scale3d(1,1,1); } }
@keyframes anim-flip { from { transform: perspective(600px) rotateX(90deg); opacity: 0; } to { transform: perspective(600px) rotateX(0); opacity: 1; } }
@keyframes anim-rotate-in { from { transform: rotate(-8deg) scale(0.98); opacity: 0; } to { transform: rotate(0) scale(1); opacity: 1; } }
@keyframes anim-skew { from { transform: skewX(-8deg) translateY(10px); opacity: 0; } to { transform: skewX(0) translateY(0); opacity: 1; } }
@keyframes anim-glow { from { box-shadow: 0 0 0 rgba(56, 189, 248, 0); } to { box-shadow: 0 14px 28px rgba(56, 189, 248, 0.35); } }
@keyframes anim-shimmer { 0% { background-position: -120% 0; } 100% { background-position: 120% 0; } }
@keyframes anim-float { 0% { transform: translateY(0); } 50% { transform: translateY(-6px); } 100% { transform: translateY(0); } }
@keyframes anim-breathe { 0% { transform: scale(0.98); } 50% { transform: scale(1.02); } 100% { transform: scale(0.98); } }
@keyframes anim-gentle-wiggle { 0% { transform: rotate(0); } 25% { transform: rotate(1.5deg); } 50% { transform: rotate(0); } 75% { transform: rotate(-1.5deg); } 100% { transform: rotate(0); } }
";
                head.AppendChild(style);
            }

            private static void EnsureFrameAnimationScript(IDocument document)
            {
                var head = document.Head;
                if (head is null)
                {
                    return;
                }

                if (document.QuerySelector("script[data-frame-anim='true']") is not null)
                {
                    return;
                }

                var script = document.CreateElement("script");
                script.SetAttribute("data-frame-anim", "true");
                                script.TextContent = @"
(function(){
    function init(){
        var items = Array.prototype.slice.call(document.querySelectorAll('[data-frame-anim]'));
        if (items.length === 0) return;

        function applyVars(el){
            var duration = el.getAttribute('data-frame-anim-duration') || '600';
            var delay = el.getAttribute('data-frame-anim-delay') || '0';
            var easing = el.getAttribute('data-frame-anim-easing') || 'ease';
            var loop = el.getAttribute('data-frame-anim-loop') === 'true';
            el.style.setProperty('--lp-frame-anim-duration', duration + 'ms');
            el.style.setProperty('--lp-frame-anim-delay', delay + 'ms');
            el.style.setProperty('--lp-frame-anim-easing', easing);
            el.style.setProperty('--lp-frame-anim-iterations', loop ? 'infinite' : '1');
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
            });
        }, { threshold: 0.2 });

        items.forEach(function(el){
            applyVars(el);
            if (el.getAttribute('data-frame-anim-trigger') === 'load') {
                el.classList.add('is-inview');
                return;
            }
            observer.observe(el);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
";
                head.AppendChild(script);
            }

            private static FrameStyle? FindFrameStyleOverrideByNormalizedKey(ContentModel content, string normalizedKey)
            {
                if (content.SectionStyles is null)
                {
                    return null;
                }

                foreach (var pair in content.SectionStyles)
                {
                    if (NormalizeSectionKey(pair.Key) != normalizedKey)
                    {
                        continue;
                    }

                    var overrideStyle = pair.Value?.FrameStyleOverride;
                    if (overrideStyle is not null)
                    {
                        return overrideStyle;
                    }
                }

                return null;
            }

            private static FrameStyle MergeFrameStyle(FrameStyle baseStyle, FrameStyle? overrideStyle)
            {
                if (overrideStyle is null)
                {
                    return baseStyle;
                }

                return new FrameStyle
                {
                    Type = string.IsNullOrWhiteSpace(overrideStyle.Type) ? baseStyle.Type : overrideStyle.Type,
                    BackgroundColor = !string.IsNullOrWhiteSpace(overrideStyle.BackgroundColor) ? overrideStyle.BackgroundColor : baseStyle.BackgroundColor,
                    BackgroundOpacity = overrideStyle.BackgroundOpacity ?? baseStyle.BackgroundOpacity,
                    BorderColor = !string.IsNullOrWhiteSpace(overrideStyle.BorderColor) ? overrideStyle.BorderColor : baseStyle.BorderColor,
                    BorderWidth = overrideStyle.BorderWidth ?? baseStyle.BorderWidth,
                    BorderRadius = overrideStyle.BorderRadius ?? baseStyle.BorderRadius,
                    ShadowLevel = !string.IsNullOrWhiteSpace(overrideStyle.ShadowLevel) ? overrideStyle.ShadowLevel : baseStyle.ShadowLevel,
                    PaddingPreset = !string.IsNullOrWhiteSpace(overrideStyle.PaddingPreset) ? overrideStyle.PaddingPreset : baseStyle.PaddingPreset,
                    PaddingX = overrideStyle.PaddingX ?? baseStyle.PaddingX,
                    PaddingY = overrideStyle.PaddingY ?? baseStyle.PaddingY,
                    MaxWidthPx = overrideStyle.MaxWidthPx ?? baseStyle.MaxWidthPx,
                    Centered = overrideStyle.Centered,
                    HeaderBackgroundColor = !string.IsNullOrWhiteSpace(overrideStyle.HeaderBackgroundColor) ? overrideStyle.HeaderBackgroundColor : baseStyle.HeaderBackgroundColor,
                    HeaderTextColor = !string.IsNullOrWhiteSpace(overrideStyle.HeaderTextColor) ? overrideStyle.HeaderTextColor : baseStyle.HeaderTextColor,
                    HeaderFontSizePx = overrideStyle.HeaderFontSizePx ?? baseStyle.HeaderFontSizePx,
                    HeaderFontFamily = !string.IsNullOrWhiteSpace(overrideStyle.HeaderFontFamily) ? overrideStyle.HeaderFontFamily : baseStyle.HeaderFontFamily,
                    HeaderHeightPx = overrideStyle.HeaderHeightPx ?? baseStyle.HeaderHeightPx,
                    HeaderRadiusTop = overrideStyle.HeaderRadiusTop,
                    BodyFontSizePx = overrideStyle.BodyFontSizePx ?? baseStyle.BodyFontSizePx,
                    BodyFontFamily = !string.IsNullOrWhiteSpace(overrideStyle.BodyFontFamily) ? overrideStyle.BodyFontFamily : baseStyle.BodyFontFamily,
                    CornerDecoration = MergeCornerDecoration(baseStyle.CornerDecoration, overrideStyle.CornerDecoration)
                };
            }

            private static CornerDecorationSet? MergeCornerDecoration(CornerDecorationSet? baseSet, CornerDecorationSet? overrideSet)
            {
                if (overrideSet is null)
                {
                    return baseSet;
                }

                return new CornerDecorationSet
                {
                    TopLeft = MergeCornerDecoration(baseSet?.TopLeft, overrideSet.TopLeft),
                    TopRight = MergeCornerDecoration(baseSet?.TopRight, overrideSet.TopRight),
                    BottomLeft = MergeCornerDecoration(baseSet?.BottomLeft, overrideSet.BottomLeft),
                    BottomRight = MergeCornerDecoration(baseSet?.BottomRight, overrideSet.BottomRight)
                };
            }

            private static CornerDecoration? MergeCornerDecoration(CornerDecoration? baseDeco, CornerDecoration? overrideDeco)
            {
                if (overrideDeco is null)
                {
                    return baseDeco;
                }

                return new CornerDecoration
                {
                    ImagePath = !string.IsNullOrWhiteSpace(overrideDeco.ImagePath) ? overrideDeco.ImagePath : baseDeco?.ImagePath,
                    SizePx = overrideDeco.SizePx ?? baseDeco?.SizePx,
                    OffsetX = overrideDeco.OffsetX ?? baseDeco?.OffsetX,
                    OffsetY = overrideDeco.OffsetY ?? baseDeco?.OffsetY,
                    RotateDeg = overrideDeco.RotateDeg ?? baseDeco?.RotateDeg,
                    Opacity = overrideDeco.Opacity ?? baseDeco?.Opacity,
                    FlipX = overrideDeco.FlipX ?? baseDeco?.FlipX,
                    FlipY = overrideDeco.FlipY ?? baseDeco?.FlipY,
                    ZIndex = overrideDeco.ZIndex ?? baseDeco?.ZIndex,
                    Inside = overrideDeco.Inside ?? baseDeco?.Inside
                };
            }

            private static void ApplyFrameStyleToElements(IElement frame, IElement header, IElement body, FrameStyle style)
            {
                var (paddingX, paddingY) = ResolveFramePadding(style);
                var borderRadius = style.BorderRadius is > 0 ? style.BorderRadius.Value : 0;

                var frameStyle = BuildFrameStyleInline(style, borderRadius);
                var headerStyle = BuildFrameHeaderInline(style, borderRadius, paddingX);
                var bodyStyle = BuildFrameBodyInline(paddingX, paddingY, style);

                AppendInlineStyle(frame, frameStyle);
                AppendInlineStyle(header, headerStyle);
                AppendInlineStyle(body, bodyStyle);
            }

            private static void ApplyCornerDecorations(IElement section, ContentModel content, string normalizedKey)
            {
                var frame = section.QuerySelector(".lp-frame") as IElement;
                if (frame is null)
                {
                    return;
                }

                var style = ResolveFrameStyle(content, normalizedKey);
                ApplyCornerDecorations(frame, style);
            }

            private static void ApplyCornerDecorations(IElement frame, FrameStyle style)
            {
                foreach (var existing in frame.QuerySelectorAll(".lp-frame-corner").ToList())
                {
                    existing.Remove();
                }

                var set = style.CornerDecoration;
                if (set is null)
                {
                    return;
                }

                var hasOutside = false;
                hasOutside |= AppendCornerDecoration(frame, set.TopLeft, "tl");
                hasOutside |= AppendCornerDecoration(frame, set.TopRight, "tr");
                hasOutside |= AppendCornerDecoration(frame, set.BottomLeft, "bl");
                hasOutside |= AppendCornerDecoration(frame, set.BottomRight, "br");

                if (hasOutside)
                {
                    AppendInlineStyle(frame, "overflow: visible");
                }
            }

            private static bool AppendCornerDecoration(IElement frame, CornerDecoration? deco, string corner)
            {
                if (deco is null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(deco.ImagePath))
                {
                    return false;
                }

                var doc = frame.Owner;
                if (doc is null)
                {
                    return false;
                }

                var img = doc.CreateElement("img");
                img.ClassList.Add("lp-frame-corner");
                img.SetAttribute("data-corner", corner);
                img.SetAttribute("src", deco.ImagePath);

                var size = Math.Clamp(deco.SizePx ?? 80, 12, 320);
                var opacity = Math.Clamp(deco.Opacity ?? 100, 0, 100) / 100d;
                var rotate = deco.RotateDeg ?? 0d;
                var flipX = deco.FlipX == true ? -1 : 1;
                var flipY = deco.FlipY == true ? -1 : 1;
                var zIndex = deco.ZIndex ?? 2;
                var inside = deco.Inside ?? true;

                var offsetX = deco.OffsetX ?? 0;
                var offsetY = deco.OffsetY ?? 0;
                var cornerLeft = corner is "tl" or "bl";
                var cornerTop = corner is "tl" or "tr";
                var xSign = cornerLeft ? 1 : -1;
                var ySign = cornerTop ? 1 : -1;
                var appliedX = inside ? offsetX : -offsetX;
                var appliedY = inside ? offsetY : -offsetY;
                var translateX = xSign * appliedX;
                var translateY = ySign * appliedY;

                var posRules = new List<string>
                {
                    "position:absolute",
                    $"width:{size}px",
                    $"height:{size}px",
                    $"opacity:{opacity:0.###}",
                    $"z-index:{zIndex}",
                    "pointer-events:none",
                    $"transform: translate({translateX}px, {translateY}px) rotate({rotate}deg) scale({flipX}, {flipY})"
                };

                if (cornerLeft)
                {
                    posRules.Add("left:0");
                }
                else
                {
                    posRules.Add("right:0");
                }

                if (cornerTop)
                {
                    posRules.Add("top:0");
                }
                else
                {
                    posRules.Add("bottom:0");
                }

                img.SetAttribute("style", string.Join("; ", posRules));
                frame.AppendChild(img);

                return !inside;
            }

            private static string BuildFrameStyleInline(FrameStyle style, int borderRadius)
            {
                var rules = new List<string> { "box-sizing: border-box" };

                var backgroundColor = SanitizeCssColor(style.BackgroundColor);
                if (!string.IsNullOrWhiteSpace(backgroundColor))
                {
                    var opacity = Math.Clamp((style.BackgroundOpacity ?? 100) / 100d, 0, 1);
                    rules.Add($"background: {ToRgba(backgroundColor, opacity)}");
                }

                if (style.BorderWidth is > 0)
                {
                    var borderColor = SanitizeCssColor(style.BorderColor) ?? "#dc2626";
                    rules.Add($"border: {style.BorderWidth}px solid {borderColor}");
                }

                if (borderRadius > 0)
                {
                    rules.Add($"border-radius: {borderRadius}px");
                    rules.Add("overflow: hidden");
                }

                var shadow = ResolveSectionShadow(style.ShadowLevel ?? "off");
                if (!string.IsNullOrWhiteSpace(shadow))
                {
                    rules.Add($"box-shadow: {shadow}");
                }

                if (style.MaxWidthPx is > 0)
                {
                    rules.Add("width: 100%");
                    rules.Add($"max-width: {style.MaxWidthPx}px");
                    rules.Add(style.Centered ? "margin: 0 auto" : "margin: 0");
                }

                return string.Join("; ", rules);
            }

            private static string BuildFrameHeaderInline(FrameStyle style, int borderRadius, int paddingX)
            {
                var rules = new List<string> { "box-sizing: border-box", "width: 100%" };

                var headerBg = SanitizeCssColor(style.HeaderBackgroundColor);
                if (!string.IsNullOrWhiteSpace(headerBg))
                {
                    rules.Add($"background: {headerBg}");
                }

                var headerText = SanitizeCssColor(style.HeaderTextColor);
                if (!string.IsNullOrWhiteSpace(headerText))
                {
                    rules.Add($"color: {headerText}");
                }

                if (style.HeaderHeightPx is > 0)
                {
                    rules.Add($"height: {style.HeaderHeightPx}px");
                    rules.Add($"min-height: {style.HeaderHeightPx}px");
                }

                rules.Add($"padding: 0 {paddingX}px");

                if (style.HeaderFontSizePx is > 0)
                {
                    rules.Add($"font-size: {style.HeaderFontSizePx}px");
                }

                var headerFont = SanitizeFontFamily(style.HeaderFontFamily);
                if (!string.IsNullOrWhiteSpace(headerFont))
                {
                    rules.Add($"font-family: {headerFont}");
                }

                if (style.HeaderRadiusTop && borderRadius > 0)
                {
                    rules.Add($"border-top-left-radius: {borderRadius}px");
                    rules.Add($"border-top-right-radius: {borderRadius}px");
                }

                return string.Join("; ", rules);
            }

            private static string BuildFrameBodyInline(int paddingX, int paddingY, FrameStyle style)
            {
                var rules = new List<string>
                {
                    $"padding: {paddingY}px {paddingX}px",
                    "box-sizing: border-box",
                    "width: 100%"
                };

                if (style.BodyFontSizePx is > 0)
                {
                    rules.Add($"font-size: {style.BodyFontSizePx}px");
                }

                var bodyFont = SanitizeFontFamily(style.BodyFontFamily);
                if (!string.IsNullOrWhiteSpace(bodyFont))
                {
                    rules.Add($"font-family: {bodyFont}");
                }

                return string.Join("; ", rules);
            }

            private static (int PaddingX, int PaddingY) ResolveFramePadding(FrameStyle style)
            {
                if (string.Equals(style.PaddingPreset, "custom", StringComparison.OrdinalIgnoreCase))
                {
                    return (Math.Clamp(style.PaddingX ?? 0, 0, 48), Math.Clamp(style.PaddingY ?? 0, 0, 48));
                }

                return style.PaddingPreset switch
                {
                    "compact" => (16, 12),
                    "spacious" => (32, 24),
                    _ => (24, 20)
                };
            }

            private static string? ResolveSectionTitle(ContentModel content, string normalizedKey, string rawKey, IElement group)
            {
                var normalized = NormalizeSectionKey(normalizedKey);
                if (string.Equals(normalized, "ranking", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
                var displayName = content.SectionGroups
                    .FirstOrDefault(g => NormalizeSectionKey(g.Key) == normalized)
                    ?.DisplayName;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                if (content.CustomSections is not null)
                {
                    var customTitle = content.CustomSections
                        .FirstOrDefault(s => NormalizeSectionKey(s.Key) == normalized)
                        ?.Title;
                    if (!string.IsNullOrWhiteSpace(customTitle))
                    {
                        return customTitle;
                    }
                }

                switch (normalized)
                {
                    case "campaigncontent":
                        return content.Sections.CampaignContent.Title;
                    case "couponnotes":
                        return content.Sections.CouponNotes.Title;
                    case "couponperiod":
                        return content.Sections.CouponPeriod.Title;
                    case "couponflow":
                        return content.Sections.CouponFlow.Title;
                    case "stickytabs":
                        return "付箋タブ";
                    case "ranking":
                        return string.Empty;
                    case "paymenthistory":
                        return string.IsNullOrWhiteSpace(content.Sections.PaymentHistory.TitleText)
                            ? content.Sections.PaymentHistory.TitleAlt
                            : content.Sections.PaymentHistory.TitleText;
                }

                if (normalized.StartsWith("storesearch", StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(content.Sections.StoreSearch.Title)
                        ? "キャンペーン対象店舗検索"
                        : content.Sections.StoreSearch.Title;
                }

                if (rawKey.StartsWith("custom-section", StringComparison.OrdinalIgnoreCase))
                {
                    return "通常セクション";
                }

                var heading = group.QuerySelector(".campaign__heading, .section-title, h2, h3")?.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(heading))
                {
                    return heading;
                }

                return rawKey;
            }

            private static void ApplySectionAnimations(IDocument document, ContentModel content)
            {
                foreach (var group in document.QuerySelectorAll(".section-group[data-section]").ToList())
                {
                    var key = group.GetAttribute("data-section");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    var animation = ResolveSectionAnimation(content, key);
                    if (animation is null)
                    {
                        continue;
                    }

                    if (string.Equals(animation.Type, "none", StringComparison.OrdinalIgnoreCase))
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
                if (TryGetSectionStyle(content, key, out var style) && style?.SectionAnimation is not null)
                {
                    return style.SectionAnimation;
                }

                if (TryGetSectionAnimation(content, key, out var animation))
                {
                    return animation;
                }

                return null;
            }

            private static bool TryGetSectionStyle(ContentModel content, string key, out SectionStyleModel? style)
            {
                style = null;
                if (content.SectionStyles is null || content.SectionStyles.Count == 0)
                {
                    return false;
                }

                if (content.SectionStyles.TryGetValue(key, out style) && style is not null)
                {
                    return true;
                }

                var normalizedKey = NormalizeSectionKey(key);
                foreach (var entry in content.SectionStyles)
                {
                    if (NormalizeSectionKey(entry.Key) == normalizedKey)
                    {
                        style = entry.Value;
                        return style is not null;
                    }
                }

                return false;
            }

            private static bool TryGetSectionAnimation(ContentModel content, string key, out SectionAnimationModel? animation)
            {
                animation = null;
                if (content.SectionAnimations is null || content.SectionAnimations.Count == 0)
                {
                    return false;
                }

                if (content.SectionAnimations.TryGetValue(key, out animation) && animation is not null)
                {
                    return true;
                }

                var normalizedKey = NormalizeSectionKey(key);
                foreach (var entry in content.SectionAnimations)
                {
                    if (NormalizeSectionKey(entry.Key) == normalizedKey)
                    {
                        animation = entry.Value;
                        return animation is not null;
                    }
                }

                return false;
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
.lp-frame-body .lp-decorations { inset: 0; }
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

/* auto-fit decoration images to frame width (opt-in) */
.lp-frame-body :where(.deco-img)[data-autofit='true'] { max-width: clamp(72px, 18vw, 240px); height: auto; position: absolute; }
.lp-frame-body :where(.deco-img)[data-autofit='true'] img { width: 100%; height: auto; object-fit: contain; display: block; }
.lp-frame-body :where(.deco-img.-left)[data-autofit='true'] { left: clamp(-10px, -2vw, -18px); top: clamp(8px, 2vw, 26px); }
.lp-frame-body :where(.deco-img.-right)[data-autofit='true'] { right: clamp(-10px, -2vw, -18px); top: clamp(6px, 1.6vw, 22px); }
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

                    var host = group.QuerySelector(".lp-frame-body") as IElement ?? group;
                    if (container.ParentElement != host)
                    {
                        host.AppendChild(container);
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

    private static void EnsureViewportMeta(IDocument document, int? fixedViewportWidth)
    {
        IElement? head = document.Head ?? document.QuerySelector("head") as IElement;
        if (head is null)
        {
            head = document.CreateElement("head");
            document.DocumentElement?.Prepend(head);
        }

        var viewport = document.QuerySelector("meta[name='viewport']");
        if (viewport is null)
        {
            viewport = document.CreateElement("meta");
            viewport.SetAttribute("name", "viewport");
            head.AppendChild(viewport);
        }

        if (fixedViewportWidth is > 0)
        {
            viewport.SetAttribute("content", $"width={fixedViewportWidth}, initial-scale=1, maximum-scale=1");
        }
        else
        {
            viewport.SetAttribute("content", "width=device-width, initial-scale=1");
        }
    }

    private static void EnsureSpResetStyle(IDocument document)
    {
        IElement? head = document.Head ?? document.QuerySelector("head") as IElement;
        if (head is null)
        {
            head = document.CreateElement("head");
            document.DocumentElement?.Prepend(head);
        }

        if (document.QuerySelector("style[data-sp-reset='true']") is not null)
        {
            return;
        }

        var style = document.CreateElement("style");
        style.SetAttribute("data-sp-reset", "true");
        style.TextContent = @"
html, body { overflow-x: hidden !important; min-width: 0 !important; }
*, *::before, *::after { box-sizing: border-box; }
img, svg, video, canvas { max-width: 100% !important; height: auto !important; }
    [class*='container'], [class*='inner'], .container, .inner, .wrap, .wrapper { max-width: 100% !important; width: 100% !important; }
    body { width: 100% !important; }
";
        head.AppendChild(style);
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

    private static string? SanitizeCssUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return uri.ToString();
            }
        }

        if (trimmed.StartsWith("/", StringComparison.Ordinal) || !trimmed.Contains(":", StringComparison.Ordinal))
        {
            return trimmed.Replace("'", "");
        }

        return null;
    }

    private static string ToRgba(string hex, double opacity)
    {
        var color = SanitizeCssColor(hex) ?? "#ffffff";
        var normalized = color.TrimStart('#');
        if (normalized.Length == 3)
        {
            normalized = string.Concat(normalized.Select(c => new string(c, 2)));
        }

        if (normalized.Length != 6
            || !int.TryParse(normalized[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !int.TryParse(normalized.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !int.TryParse(normalized.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return color;
        }

        var alpha = Math.Max(0, Math.Min(1, opacity));
        return $"rgba({r}, {g}, {b}, {alpha:0.##})";
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
        ApplySectionFont(document, "coupon-flow", content.Sections.CouponFlow.FontFamily);
        ApplySectionFont(document, "couponFlow", content.Sections.CouponFlow.FontFamily);
        ApplySectionFont(document, "sticky-tabs", content.Sections.StickyTabs.FontFamily);
        ApplySectionFont(document, "stickyTabs", content.Sections.StickyTabs.FontFamily);
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
            foreach (var element in document.QuerySelectorAll("footer, .footer, .mv-footer").ToList())
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
        ApplySectionTextAlign(document, "coupon-flow", content.Sections.CouponFlow.TextAlign);
        ApplySectionTextAlign(document, "couponFlow", content.Sections.CouponFlow.TextAlign);
        ApplySectionTextAlign(document, "sticky-tabs", content.Sections.StickyTabs.TextAlign);
        ApplySectionTextAlign(document, "stickyTabs", content.Sections.StickyTabs.TextAlign);
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
        if (mvFooterColor is null)
        {
            var frameStyle = content.FrameDefaultStyle ?? content.CardThemeStyle;
            mvFooterColor = SanitizeCssColor(frameStyle?.HeaderBackgroundColor);
        }
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
            rules.Add($".section-group[data-section='countdown'] .section-title {{ background-color: {mvFooterColor} !important; }}");
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
        var heroSp = heroPicture?.QuerySelectorAll("source[media]")
            ?.FirstOrDefault(source =>
                (source.GetAttribute("media") ?? string.Empty)
                    .Contains("max-width", StringComparison.OrdinalIgnoreCase));

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

                if (!IsImageDeleted(content, content.Hero.ImageSp))
                {
                        var head = document.Head ?? document.QuerySelector("head") as IElement;
                        var spUrl = ResolveImageUrl(content.Hero.ImageSp, template, overrides, embedImages);

                        if (heroPc is not null)
                        {
                                var existingSpImg = document.QuerySelector(".mv img.mv-sp-preview") as IElement;
                                if (existingSpImg is null)
                                {
                                        var spImg = document.CreateElement("img");
                                        spImg.ClassList.Add("mv-sp-preview");
                                        spImg.SetAttribute("src", spUrl);
                                        spImg.SetAttribute("alt", content.Hero.Alt ?? string.Empty);
                                        heroPc.ParentElement?.InsertBefore(spImg, heroPc);
                                }
                        }

                        if (head is not null && document.QuerySelector("style[data-hero-sp='true']") is null)
                        {
                                var style = document.CreateElement("style");
                                style.SetAttribute("data-hero-sp", "true");
                                style.TextContent = @$"@media (max-width: 768px) {{
    .mv, .mv-wrap, .mv__wrap, .mv__wrapper, .hero, .hero-wrap, .hero__wrap {{
        height: auto !important;
        min-height: 0 !important;
        overflow: visible !important;
        background-image: none !important;
    }}
    .mv img.mv-sp-preview {{ display: block !important; width: 100% !important; height: auto !important; }}
    .mv picture, .mv img:not(.mv-sp-preview) {{ display: none !important; }}
    .mv, .mv-wrap, .mv__wrap, .mv__wrapper, .hero, .hero-wrap, .hero__wrap {{
        background-size: contain !important;
        background-repeat: no-repeat !important;
        background-position: center top !important;
        background-image: url('{spUrl}') !important;
    }}
}}";
                                head.AppendChild(style);
                        }
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
        EnsurePaymentHistorySection(document, content, template, overrides, embedImages);
        EnsureStoreSearchSection(document, content, template, overrides, embedImages);
        EnsureCouponFlowSection(document, content, template, overrides, embedImages);
        EnsureStickyTabsSection(document, content, template, overrides, embedImages);
        ApplySectionGroups(document, template, content);

        var campaignContentSection = document.QuerySelector(".section-group[data-section='campaign-content']")
            ?? document.QuerySelector(".section-group[data-section='campaignContent']");
        var couponNotesSection = document.QuerySelector(".section-group[data-section='coupon-notes']")
            ?? document.QuerySelector(".section-group[data-section='couponNotes']");
        var couponPeriodSection = document.QuerySelector(".section-group[data-section='coupon-period']")
            ?? document.QuerySelector(".section-group[data-section='couponPeriod']");
        var couponFlowSection = document.QuerySelector(".section-group[data-section='coupon-flow']")
            ?? document.QuerySelector(".section-group[data-section='couponFlow']");
        var storeSearchSection = document.QuerySelector(".section-group[data-section='store-search']")
            ?? document.QuerySelector(".section-group[data-section='storeSearch']");
        var rankingSection = document.QuerySelector(".section-group[data-section='ranking']");
        var paymentHistorySection = document.QuerySelector(".section-group[data-section='payment-history']")
            ?? document.QuerySelector(".section-group[data-section='paymentHistory']");
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

        if (couponFlowSection is not null
            && !content.Sections.CouponFlow.Enabled)
        {
            couponFlowSection.Remove();
            couponFlowSection = null;
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

        if (paymentHistorySection is not null
            && !content.Sections.PaymentHistory.Enabled)
        {
            paymentHistorySection.Remove();
            paymentHistorySection = null;
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
        var hasCountdownGroup = content.SectionGroups.Any(group => NormalizeSectionKey(group.Key) == "countdown");
        if (!hasCountdownGroup)
        {
            if (countdownSection is not null)
            {
                countdownSection.Remove();
                countdownSection = null;
            }
            HideCountdownElements(document, dateRangeText);
        }
        else
        {
            ApplyCountdownDisplay(document, content, dateRangeText);
        }

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

        if (couponFlowSection is not null && content.Sections.CouponFlow.Enabled)
        {
            UpdateCouponFlowSection(couponFlowSection, content, template, overrides, embedImages);
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

        if (content.Sections.PaymentHistory.Enabled)
        {
            if (paymentHistorySection is not null)
            {
                UpdatePaymentHistorySection(paymentHistorySection, content, template, overrides, embedImages);
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

        private static void EnsurePaymentHistorySection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (!content.Sections.PaymentHistory.Enabled)
            {
                return;
            }

            var paymentSection = document.QuerySelector(".section-group[data-section='payment-history']")
                ?? document.QuerySelector(".section-group[data-section='paymentHistory']");
            if (paymentSection is null)
            {
                var parent = GetSectionGroupParent(document);
                if (parent is null)
                {
                    return;
                }

                paymentSection = CreatePaymentHistorySection(document, content, template, overrides, embedImages);
                parent.AppendChild(paymentSection);
            }
            else
            {
                UpdatePaymentHistorySection(paymentSection, content, template, overrides, embedImages);
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

        private static void EnsureCouponFlowSection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (!content.Sections.CouponFlow.Enabled)
            {
                return;
            }

            var couponFlowSection = document.QuerySelector(".section-group[data-section='coupon-flow']")
                ?? document.QuerySelector(".section-group[data-section='couponFlow']");
            if (couponFlowSection is null)
            {
                var parent = GetSectionGroupParent(document);
                if (parent is null)
                {
                    return;
                }

                couponFlowSection = CreateCouponFlowSection(document, content, template, overrides, embedImages);
                parent.AppendChild(couponFlowSection);
            }
            else
            {
                UpdateCouponFlowSection(couponFlowSection, content, template, overrides, embedImages);
            }
        }

        private static void EnsureStickyTabsSection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (!content.Sections.StickyTabs.Enabled)
            {
                return;
            }

            var stickySection = document.QuerySelector(".section-group[data-section='sticky-tabs']")
                ?? document.QuerySelector(".section-group[data-section='stickyTabs']");
            if (stickySection is null)
            {
                var parent = GetSectionGroupParent(document);
                if (parent is null)
                {
                    return;
                }

                stickySection = CreateStickyTabsSection(document, content, template, overrides, embedImages);
                parent.AppendChild(stickySection);
            }
            else
            {
                UpdateStickyTabsSection(stickySection, content, template, overrides, embedImages);
            }
        }

        private static IElement CreateStickyTabsSection(
            IDocument document,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            var section = document.CreateElement("section");
            section.ClassList.Add("section-group", "sticky-tabs-section");
            section.SetAttribute("data-section", "sticky-tabs");
            section.InnerHtml = BuildStickyTabsSectionHtml(content, template, overrides, embedImages);
            return section;
        }

        private static void UpdateStickyTabsSection(
            IElement section,
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            section.ClassList.Add("sticky-tabs-section");
            section.InnerHtml = BuildStickyTabsSectionHtml(content, template, overrides, embedImages);
        }

        private static string BuildStickyTabsSectionHtml(
            ContentModel content,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            var tabs = content.Sections.StickyTabs.Tabs ?? new List<StickyTabModel>();
            if (tabs.Count == 0)
            {
                return "<div class=\"sticky-tabs\" data-sticky-tabs=\"true\"><div class=\"sticky-tabs__empty\">タブを追加してください。</div></div>";
            }

            var tabButtons = new StringBuilder();
            var panes = new StringBuilder();
            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var title = WebUtility.HtmlEncode(tab.Title ?? string.Empty);
                var bg = string.IsNullOrWhiteSpace(tab.Color) ? "#e5e7eb" : tab.Color;
                var textColor = ResolveStickyTabTextColor(bg, tab.TextColor);
                var activeClass = i == 0 ? " is-active" : string.Empty;
                tabButtons.Append($"<button type=\"button\" class=\"sticky-tabs__tab{activeClass}\" data-tab-index=\"{i}\" style=\"background:{bg};color:{textColor};\" aria-selected=\"{(i == 0 ? "true" : "false")}\">{title}</button>");

                var paneClass = i == 0 ? "sticky-tabs__pane is-active" : "sticky-tabs__pane";
                panes.Append($"<div class=\"{paneClass}\" data-tab-index=\"{i}\">{BuildStickyTabContentHtml(tab, template, overrides, embedImages)}</div>");
            }

                        return $@"
<div class=""sticky-tabs"" data-sticky-tabs=""true"">
    <div class=""sticky-tabs__bar"">
        <div class=""sticky-tabs__tabs"">{tabButtons}</div>
    </div>
    <div class=""sticky-tabs__panes"">{panes}</div>
</div>";
        }

        private static string BuildStickyTabContentHtml(
            StickyTabModel tab,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            if (tab.ContentBlocks is null || tab.ContentBlocks.Count == 0)
            {
                return "<div class=\"sticky-tabs__empty\">内容を追加してください。</div>";
            }

            var blocks = new StringBuilder();
            foreach (var block in tab.ContentBlocks)
            {
                blocks.Append(BuildStickyBlockHtml(block, template, overrides, embedImages));
            }

            return blocks.ToString();
        }

        private static string BuildStickyBlockHtml(
            StickyBlockModel block,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages)
        {
            switch (block.Type)
            {
                case StickyBlockType.Image:
                    return BuildStickyImageBlock(block, template, overrides, embedImages, includeCaption: false);
                case StickyBlockType.ImageWithCaption:
                    return BuildStickyImageBlock(block, template, overrides, embedImages, includeCaption: true);
                case StickyBlockType.Divider:
                    return "<hr class=\"sticky-tabs__divider\" />";
                default:
                    var html = NormalizeStickyText(block.RichTextHtml);
                    return $"<div class=\"sticky-tabs__text\">{html}</div>";
            }
        }

        private static string BuildStickyImageBlock(
            StickyBlockModel block,
            TemplateProject template,
            IDictionary<string, byte[]>? overrides,
            bool embedImages,
            bool includeCaption)
        {
            if (string.IsNullOrWhiteSpace(block.ImageSrc))
            {
                return "<div class=\"sticky-tabs__image empty\">画像を指定してください。</div>";
            }

            var src = ResolveImageUrl(block.ImageSrc, template, overrides, embedImages);
            var caption = includeCaption && !string.IsNullOrWhiteSpace(block.Caption)
                ? $"<figcaption class=\"sticky-tabs__caption\">{WebUtility.HtmlEncode(block.Caption)}</figcaption>"
                : string.Empty;

            return $"<figure class=\"sticky-tabs__image\"><img src=\"{src}\" alt=\"\" />{caption}</figure>";
        }

        private static string NormalizeStickyText(string? text)
        {
            return WebUtility.HtmlEncode(text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\n", "<br />", StringComparison.Ordinal);
        }

        private static string ResolveStickyTabTextColor(string? backgroundColor, string? manualTextColor)
        {
            if (!string.IsNullOrWhiteSpace(manualTextColor))
            {
                return manualTextColor;
            }

            if (!TryParseHexColor(backgroundColor, out var r, out var g, out var b))
            {
                return "#111827";
            }

            var luminance = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255d;
            return luminance > 0.6 ? "#111827" : "#ffffff";
        }

        private static bool TryParseHexColor(string? value, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var hex = value.Trim();
            if (hex.StartsWith('#'))
            {
                hex = hex[1..];
            }

            if (hex.Length == 3)
            {
                hex = string.Concat(hex.Select(ch => string.Concat(ch, ch)));
            }

            if (hex.Length != 6)
            {
                return false;
            }

            return int.TryParse(hex.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out r)
                && int.TryParse(hex.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out g)
                && int.TryParse(hex.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out b);
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

                private static void EnsureCouponFlowStyle(IDocument document, ContentModel content)
                {
                        if (!content.Sections.CouponFlow.Enabled)
                        {
                                return;
                        }

                        var head = document.Head;
                        if (head is null || document.QuerySelector("style[data-coupon-flow-style='true']") is not null)
                        {
                                return;
                        }

                        var style = document.CreateElement("style");
                        style.SetAttribute("data-coupon-flow-style", "true");
                        style.TextContent = @"
.coupon-flow-section .coupon-howto-inner { padding: 4px 0 8px; overflow-x: hidden; }
.coupon-flow-section .coupon-howto-lead { margin: 8px 0 12px; }
.coupon-flow-section .coupon-howto-lead strong { color: #e50012; }
.coupon-flow-section .coupon-howto-swiper { position: relative; width: 100%; overflow-x: hidden; }
.coupon-flow-section .coupon-howto-swiper .swiper-wrapper { position: relative; }
.coupon-flow-section .coupon-howto-swiper .swiper-slide { position: absolute; inset: 0; text-align: center; opacity: 0; pointer-events: none; transition: opacity .4s ease; }
.coupon-flow-section .coupon-howto-swiper .swiper-slide.is-active { position: relative; opacity: 1; pointer-events: auto; }
.coupon-flow-section .coupon-howto-swiper img { margin: auto; max-width: min(100%, 520px); width: 100%; height: auto; }
.coupon-flow-section .coupon-flow-empty { padding: 40px 0; text-align: center; color: #6b7280; }
.coupon-flow-section .swiper-pagination { display: flex; justify-content: center; width: 100%; padding: 14px 8px; }
.coupon-flow-section .swiper-pagination-bullet { display: block; background: #ccc; width: 12px; height: 12px; border-radius: 100%; margin: 0 6px; cursor: pointer; transition: .2s; }
.coupon-flow-section .swiper-pagination-bullet-active { background: var(--coupon-flow-accent, #ea5504) !important; transition: .2s; }
.coupon-flow-section .swiper-button-prev, .coupon-flow-section .swiper-button-next { position: absolute; top: 50%; z-index: 10; display: block; cursor: pointer; background: none; width: 44px; height: 44px; transition: .2s; }
.coupon-flow-section .swiper-button-prev { left: 8px; }
.coupon-flow-section .swiper-button-next { right: 8px; }
.coupon-flow-section .swiper-button-disabled { pointer-events: none; filter: grayscale(100%); opacity: 0.2; }
.coupon-flow-section .swiper-button-prev:before,
.coupon-flow-section .swiper-button-next:before { content: ''; display: block; background: #FFF; box-sizing: border-box; width: 36px; height: 36px; border: 2px solid var(--coupon-flow-accent, #ea5504); border-radius: 50%; position: absolute; opacity: 0.8; }
.coupon-flow-section .swiper-button-prev:before { left: 0; }
.coupon-flow-section .swiper-button-next:before { right: 1px; }
.coupon-flow-section .swiper-button-prev:after,
.coupon-flow-section .swiper-button-next:after { content: ''; display: block; width: 10px; height: 10px; position: absolute; top: 13px; opacity: 0.8; }
.coupon-flow-section .swiper-button-prev:after { border-top: 4px solid var(--coupon-flow-accent, #ea5504); border-left: 4px solid var(--coupon-flow-accent, #ea5504); transform: rotate(-45deg); left: 14px; }
.coupon-flow-section .swiper-button-next:after { border-top: 4px solid var(--coupon-flow-accent, #ea5504); border-right: 4px solid var(--coupon-flow-accent, #ea5504); transform: rotate(45deg); right: 14px; }
.coupon-flow-section .note { font-size: 18px; text-align: center; margin-bottom: 0.8em; }
.coupon-flow-section .c-btn { font-weight: 700; text-align: center; line-height: 1.5; margin-left: auto; margin-right: auto; }
.coupon-flow-section .c-btn a { width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; color: var(--coupon-flow-accent, #ea5504); border-radius: 2em; border: solid 1px var(--coupon-flow-accent, #ea5504); padding: .2em .5em; background-color: white; text-decoration: none; }
.coupon-flow-section .c-btn.-border a { background-color: var(--coupon-flow-accent, #ea5504); border: 3px solid white; box-shadow: 0 0 0 3px var(--coupon-flow-accent, #ea5504); color: white; }
.coupon-flow-section .c-btn.-l { font-size: 2rem; width: 100%; max-width: 520px; height: 48px; }
.coupon-flow-section .indent { padding-left: 1em; text-indent: -1em; }
@media (max-width: 767px) {
    .coupon-flow-section .note { font-size: 1.5rem; }
}
";
                        head.AppendChild(style);
                }

                    private static void EnsureStickyTabsStyle(IDocument document, ContentModel content)
                    {
                        if (!content.Sections.StickyTabs.Enabled)
                        {
                            return;
                        }

                        var head = document.Head;
                        if (head is null || document.QuerySelector("style[data-sticky-tabs-style='true']") is not null)
                        {
                            return;
                        }

                        var style = document.CreateElement("style");
                        style.SetAttribute("data-sticky-tabs-style", "true");
                        style.TextContent = @"
                .sticky-tabs-section .sticky-tabs { display: flex; flex-direction: column; gap: 14px; }
                .sticky-tabs-section .sticky-tabs__bar { position: sticky; top: 0; z-index: 2; background: #fff; padding: 8px 0; }
                .sticky-tabs-section .sticky-tabs__tabs { display: flex; gap: 10px; flex-wrap: wrap; }
                .sticky-tabs-section .sticky-tabs__tab { border: none; padding: 10px 16px; border-radius: 14px 14px 8px 8px; font-weight: 700; cursor: pointer; box-shadow: 0 4px 10px rgba(15,23,42,0.08); }
                .sticky-tabs-section .sticky-tabs__tab.is-active { box-shadow: 0 8px 18px rgba(15,23,42,0.12); transform: translateY(-1px); }
                .sticky-tabs-section .sticky-tabs__panes { display: flex; flex-direction: column; gap: 12px; }
                .sticky-tabs-section .sticky-tabs__pane { display: none; }
                .sticky-tabs-section .sticky-tabs__pane.is-active { display: block; }
                .sticky-tabs-section .sticky-tabs__text { line-height: 1.7; }
                .sticky-tabs-section .sticky-tabs__image { margin: 0; display: flex; flex-direction: column; gap: 6px; }
                .sticky-tabs-section .sticky-tabs__image img { width: 100%; height: auto; border-radius: 10px; }
                .sticky-tabs-section .sticky-tabs__caption { font-size: 0.9rem; color: #475569; }
                .sticky-tabs-section .sticky-tabs__divider { border: none; border-top: 1px solid #e2e8f0; margin: 12px 0; }
                .sticky-tabs-section .sticky-tabs__empty { color: #64748b; font-size: 0.92rem; }
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

        var style = document.CreateElement("style");
        style.SetAttribute("data-ranking-style", "true");
        var frameStyle = ResolveFrameStyle(content, "ranking");
        var rankBackground = SanitizeCssColor(frameStyle.HeaderBackgroundColor) ?? "#BF1D20";
                var css = @"
        .lp-section[data-section-type='ranking'] .ranking-inner { background-color: __RANK_BG__; padding: 30px 55px 50px; max-width: 100%; width: 100%; display: block; margin: 0 auto; position: relative; box-sizing: border-box; overflow-x: hidden; }
.lp-section[data-section-type='ranking'] .campaign__rank-text { margin-top: 20px; font-family: ""M PLUS 1"", ""Noto Sans JP"", sans-serif; color: #fff; font-weight: 800; text-align: center; line-height: 1.2; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading { font-size: 32px; display: flex; align-items: flex-end; justify-content: center; gap: 2px; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::before, .lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::after { display: inline-block; content: ""; height: 29px; width: 15px; background: url(images/deco-rank01.png) no-repeat center bottom/100% 100%; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::before { margin-right: 10px; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::after { transform: scale(-1, 1); }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-kikan { display: flex; align-items: center; justify-content: center; gap: 10px; font-size: 22px; margin-top: 12px; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-kikan > span { background: white; border-radius: 4px; color: #BF1D20; display: inline-block; padding: 0 7px 2px; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-date { display: flex; align-items: center; justify-content: center; gap: 12px; font-size: 30px; margin-top: 24px; }
.lp-section[data-section-type='ranking'] .campaign__rank-text > .-date::before, .lp-section[data-section-type='ranking'] .campaign__rank-text > .-date::after { display: inline-block; content: ""; height: 10px; flex: 1; background: url(images/deco-rank02.png) no-repeat center center/100% 100%; }
.lp-section[data-section-type='ranking'] .campaign__rank-notes { margin-top: 25px; }
.lp-section[data-section-type='ranking'] .campaign__rank-notes li { color: #fff; }
.lp-section[data-section-type='ranking'] .campaign__rank-box { width: calc(100% + 16px); margin-left: -8px; margin-top: 12px; border-collapse: separate; border-spacing: 8px; font-family: ""M PLUS 1"", ""Noto Sans JP"", sans-serif; font-weight: 800; line-height: 1.4; }
.lp-section[data-section-type='ranking'] .campaign__rank-box th, .lp-section[data-section-type='ranking'] .campaign__rank-box td { padding: 10px 25px; }
.lp-section[data-section-type='ranking'] .campaign__rank-box th { background-color: #FFF3B1; font-size: 28px; text-align: center; }
.lp-section[data-section-type='ranking'] .campaign__rank-box td { position: relative; background-color: #fff; font-size: 32px; }
.lp-section[data-section-type='ranking'] .campaign__rank-box td.-number { font-size: 30px; text-align: center; }
.lp-section[data-section-type='ranking'] .campaign__rank-box td:not(.-number) { text-align: end; }
.lp-section[data-section-type='ranking'] .campaign__rank-box .king { z-index: 5; line-height: 1; vertical-align: bottom; text-shadow: 2px 2px 0 white, -2px 2px 0 white, 2px -2px 0 white, -2px -2px 0 white; }
.lp-section[data-section-type='ranking'] .campaign__rank-box .king::after { position: absolute; content: ""; width: 72px; height: 56px; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: -1; }
.lp-section[data-section-type='ranking'] .campaign__rank-box .king-01::after { background: url(images/icon-king-01.svg) no-repeat center center/100% auto; }
.lp-section[data-section-type='ranking'] .campaign__rank-box .king-02::after { background: url(images/icon-king-02.svg) no-repeat center center/100% auto; }
.lp-section[data-section-type='ranking'] .campaign__rank-box .king-03::after { background: url(images/icon-king-03.svg) no-repeat center center/100% auto; }
.lp-section[data-section-type='ranking'] .deco-img { position: absolute; content: ""; z-index: 2; }
.lp-section[data-section-type='ranking'] .deco-img img { height: 100%; width: auto; object-fit: contain; }
.lp-section[data-section-type='ranking'] .deco-img.-left { top: 36px; left: 32px; }
.lp-section[data-section-type='ranking'] .deco-img.-right { top: 12px; right: -16px; }
.lp-section[data-section-type='ranking'] .deco-img.-deco04 { max-width: 183px; height: auto; }
.lp-section[data-section-type='ranking'] .deco-img.-deco05 { max-width: 236px; height: auto; }
@media (max-width: 767px) {
    .lp-section[data-section-type='ranking'] .ranking-inner { padding: 30px 15px 30px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading { font-size: 18px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::before, .lp-section[data-section-type='ranking'] .campaign__rank-text > .-heading::after { width: 10px; height: 20px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-text > .-kikan { font-size: 16px; gap: 6px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-text > .-date { font-size: 18px; margin-top: 20px; gap: 8px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box { border-spacing: 3px; width: calc(100% + 8px); margin-left: -4px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box th { font-size: 16px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box td { font-size: 18px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box td.-number { font-size: 16px; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box .king { text-shadow: 1px 1px 0 white, -1px 1px 0 white, 1px -1px 0 white, -1px -1px 0 white; }
    .lp-section[data-section-type='ranking'] .campaign__rank-box .king::after { width: 56px; height: 40px; }
    .lp-section[data-section-type='ranking'] .deco-img.-deco04 { width: 80px; left: 0; top: -20px; height: 60px; }
    .lp-section[data-section-type='ranking'] .deco-img.-deco05 { width: 90px; right: 0; top: -28px; height: 65px; }
}
";
    style.TextContent = css.Replace("__RANK_BG__", rankBackground, StringComparison.Ordinal);
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
            if (pageInfo) pageInfo.textContent = currentPage + ' / ' + totalPages;
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

    private static void EnsureCouponFlowScript(IDocument document, ContentModel content)
    {
        if (!content.Sections.CouponFlow.Enabled)
        {
            return;
        }

        var head = document.Head;
        if (head is null || document.QuerySelector("script[data-coupon-flow-script='true']") is not null)
        {
            return;
        }

        var script = document.CreateElement("script");
        script.SetAttribute("data-coupon-flow-script", "true");
        script.TextContent = @"
(function(){
    function init(section){
        var swiper = section.querySelector('.coupon-flow-swiper');
        if (!swiper) return;
        var slides = Array.prototype.slice.call(swiper.querySelectorAll('.swiper-slide'));
        var bullets = Array.prototype.slice.call(section.querySelectorAll('.swiper-pagination-bullet'));
        var prev = section.querySelector('.swiper-button-prev');
        var next = section.querySelector('.swiper-button-next');
        var index = 0;
        var timer = null;
        var intervalMs = 4000;

        function clampIndex(nextIndex){
            if (slides.length === 0) return 0;
            if (nextIndex < 0) return slides.length - 1;
            if (nextIndex >= slides.length) return 0;
            return nextIndex;
        }

        function update(nextIndex){
            if (slides.length === 0) return;
            index = clampIndex(nextIndex);
            slides.forEach(function(slide, i){ slide.classList.toggle('is-active', i === index); });
            bullets.forEach(function(bullet, i){
                bullet.classList.toggle('swiper-pagination-bullet-active', i === index);
                bullet.setAttribute('aria-current', i === index ? 'true' : 'false');
            });
            if (prev) prev.classList.toggle('swiper-button-disabled', slides.length <= 1);
            if (next) next.classList.toggle('swiper-button-disabled', slides.length <= 1);
        }

        function nextSlide(){
            update(index + 1);
        }

        function startAuto(){
            if (timer || slides.length <= 1) return;
            timer = setInterval(nextSlide, intervalMs);
        }

        function stopAuto(){
            if (timer) { clearInterval(timer); timer = null; }
        }

        if (prev) prev.addEventListener('click', function(){ update(index - 1); });
        if (next) next.addEventListener('click', function(){ update(index + 1); });
        bullets.forEach(function(bullet, i){ bullet.addEventListener('click', function(){ update(i); }); });
        swiper.addEventListener('mouseenter', stopAuto);
        swiper.addEventListener('mouseleave', startAuto);
        update(0);
        startAuto();
    }

    function boot(){
        document.querySelectorAll('.section-group.coupon-flow-section').forEach(init);
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

    private static void EnsureStickyTabsScript(IDocument document, ContentModel content)
    {
        if (!content.Sections.StickyTabs.Enabled)
        {
            return;
        }

        var head = document.Head;
        if (head is null || document.QuerySelector("script[data-sticky-tabs-script='true']") is not null)
        {
            return;
        }

        var script = document.CreateElement("script");
        script.SetAttribute("data-sticky-tabs-script", "true");
        script.TextContent = @"
(function(){
    function init(section){
        var tabs = Array.prototype.slice.call(section.querySelectorAll('.sticky-tabs__tab'));
        var panes = Array.prototype.slice.call(section.querySelectorAll('.sticky-tabs__pane'));
        if (tabs.length === 0 || panes.length === 0) return;

        function activate(index){
            tabs.forEach(function(tab, i){
                var active = i === index;
                tab.classList.toggle('is-active', active);
                tab.setAttribute('aria-selected', active ? 'true' : 'false');
            });
            panes.forEach(function(pane, i){
                var active = i === index;
                pane.classList.toggle('is-active', active);
            });
        }

        tabs.forEach(function(tab, i){
            tab.addEventListener('click', function(){ activate(i); });
        });

        activate(0);
    }

    function boot(){
        document.querySelectorAll('.section-group.sticky-tabs-section').forEach(init);
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

        private static void EnsureCustomSectionGalleryStyle(IDocument document, ContentModel content)
        {
            if (content.CustomSections is null || content.CustomSections.Count == 0)
            {
                return;
            }

            var head = document.Head;
            if (head is null || document.QuerySelector("style[data-custom-gallery='true']") is not null)
            {
                return;
            }

            var style = document.CreateElement("style");
            style.SetAttribute("data-custom-gallery", "true");
            style.TextContent = @"
    .custom-section-image img { width: 100%; border-radius: 12px; box-shadow: 0 10px 20px rgba(15, 23, 42, 0.12); }
    .custom-gallery { margin: 12px 0; }
    .custom-gallery--carousel { position: relative; overflow: hidden; border-radius: 14px; background: #f8fafc; border: 1px solid rgba(148, 163, 184, 0.35); }
    .custom-gallery-track { display: flex; transition: transform 0.35s ease; }
    .custom-gallery-slide { min-width: 100%; }
    .custom-gallery-slide img { width: 100%; display: block; border-radius: 0; }
    .custom-gallery-link { display: block; }
    .custom-gallery-nav { position: absolute; top: 50%; transform: translateY(-50%); width: 36px; height: 36px; border-radius: 999px; border: 1px solid rgba(148, 163, 184, 0.5); background: rgba(255, 255, 255, 0.9); color: #0f172a; font-weight: 700; cursor: pointer; }
    .custom-gallery-nav:disabled { opacity: 0.4; cursor: not-allowed; }
    .custom-gallery-prev { left: 10px; }
    .custom-gallery-next { right: 10px; }
    .custom-gallery-dots { display: flex; justify-content: center; gap: 6px; padding: 8px 0 10px; background: #f8fafc; }
    .custom-gallery-dot { width: 8px; height: 8px; border-radius: 999px; border: none; background: #cbd5e1; cursor: pointer; }
    .custom-gallery-dot.is-active { background: #0f172a; }
    .custom-gallery--grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .custom-gallery--grid .custom-gallery-item img { width: 100%; border-radius: 12px; border: 1px solid rgba(148, 163, 184, 0.35); background: #fff; }
    @media (max-width: 768px) { .custom-gallery--grid { grid-template-columns: 1fr; } }
";
            head.AppendChild(style);
        }

        private static void EnsureCustomSectionGalleryScript(IDocument document, ContentModel content)
        {
            if (content.CustomSections is null || content.CustomSections.Count == 0)
            {
                return;
            }

            var head = document.Head;
            if (head is null || document.QuerySelector("script[data-custom-gallery-script='true']") is not null)
            {
                return;
            }

            var script = document.CreateElement("script");
            script.SetAttribute("data-custom-gallery-script", "true");
            script.TextContent = @"
    (function(){
        function initCarousel(root){
            var track = root.querySelector('.custom-gallery-track');
            var slides = Array.prototype.slice.call(root.querySelectorAll('.custom-gallery-slide'));
            if (!track || slides.length === 0) return;

            var prev = root.querySelector('.custom-gallery-prev');
            var next = root.querySelector('.custom-gallery-next');
            var dots = root.querySelector('.custom-gallery-dots');

            var showArrows = root.getAttribute('data-show-arrows') !== 'false';
            var showDots = root.getAttribute('data-show-dots') !== 'false';
            var loop = root.getAttribute('data-loop') !== 'false';
            var autoplay = root.getAttribute('data-autoplay') === 'true';
            var interval = parseInt(root.getAttribute('data-interval') || '4000', 10);
            if (!Number.isFinite(interval) || interval < 1000) interval = 4000;

            var index = 0;
            var timer = null;

            function renderDots(){
                if (!dots) return;
                dots.innerHTML = '';
                slides.forEach(function(_, i){
                    var btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'custom-gallery-dot' + (i === index ? ' is-active' : '');
                    btn.setAttribute('data-dot', i);
                    dots.appendChild(btn);
                });
            }

            function update(){
                track.style.transform = 'translateX(' + (-index * 100) + '%)';
                if (dots){
                    Array.prototype.slice.call(dots.querySelectorAll('.custom-gallery-dot')).forEach(function(dot, i){
                        if (i === index) dot.classList.add('is-active');
                        else dot.classList.remove('is-active');
                    });
                }
                if (!loop){
                    if (prev) prev.disabled = index <= 0;
                    if (next) next.disabled = index >= slides.length - 1;
                }
            }

            function go(nextIndex){
                if (nextIndex < 0){
                    if (!loop) return;
                    nextIndex = slides.length - 1;
                }
                if (nextIndex >= slides.length){
                    if (!loop) return;
                    nextIndex = 0;
                }
                index = nextIndex;
                update();
            }

            if (!showArrows){
                if (prev) prev.style.display = 'none';
                if (next) next.style.display = 'none';
            }
            if (!showDots && dots){
                dots.style.display = 'none';
            }

            renderDots();
            update();

            if (prev) prev.addEventListener('click', function(){ go(index - 1); });
            if (next) next.addEventListener('click', function(){ go(index + 1); });
            if (dots){
                dots.addEventListener('click', function(ev){
                    var target = ev.target;
                    if (!target || !target.getAttribute) return;
                    var value = target.getAttribute('data-dot');
                    if (value === null) return;
                    var i = parseInt(value, 10);
                    if (!Number.isFinite(i)) return;
                    go(i);
                });
            }

            function start(){
                if (!autoplay || slides.length <= 1) return;
                stop();
                timer = setInterval(function(){ go(index + 1); }, interval);
            }

            function stop(){
                if (timer) { clearInterval(timer); timer = null; }
            }

            root.addEventListener('mouseenter', stop);
            root.addEventListener('mouseleave', start);
            start();
        }

        function boot(){
            document.querySelectorAll('.custom-gallery[data-carousel=""true""]').forEach(initCarousel);
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
        wrapper.ClassName = "section-group ranking-section rank-ref";
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

    private static IElement CreateCouponFlowSection(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var wrapper = document.CreateElement("div");
        wrapper.ClassName = "section-group coupon-flow-section";
        wrapper.SetAttribute("data-section", "coupon-flow");
        wrapper.InnerHtml = BuildCouponFlowSectionHtml(content, template, overrides, embedImages);
        return wrapper;
    }

    private static IElement CreatePaymentHistorySection(
        IDocument document,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var wrapper = document.CreateElement("div");
        wrapper.ClassName = "section-group payment-history-section";
        wrapper.SetAttribute("data-section", "payment-history");
        wrapper.InnerHtml = BuildPaymentHistorySectionHtml(content, template, overrides, embedImages);
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
        section.ClassList.Add("rank-ref");
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

    private static void UpdateCouponFlowSection(
        IElement section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        section.ClassList.Add("coupon-flow-section");
        section.InnerHtml = BuildCouponFlowSectionHtml(content, template, overrides, embedImages);
    }

    private static void UpdatePaymentHistorySection(
        IElement section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        section.ClassList.Add("payment-history-section");
        section.InnerHtml = BuildPaymentHistorySectionHtml(content, template, overrides, embedImages);
    }

    private static string BuildPaymentHistorySectionHtml(
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var model = content.Sections.PaymentHistory;
        var textStyle = BuildPaymentHistoryTextStyle(model, "text");
        var textStyleAttr = string.IsNullOrWhiteSpace(textStyle) ? string.Empty : $" style=\"{textStyle}\"";
        var textHtml = string.IsNullOrWhiteSpace(model.Text)
            ? string.Empty
            : $"<p class=\"campaign__text\"{textStyleAttr}>{model.Text}</p>";

        var importantStyle = BuildPaymentHistoryTextStyle(model, "important");
        var importantStyleAttr = string.IsNullOrWhiteSpace(importantStyle) ? string.Empty : $" style=\"{importantStyle}\"";
        var importantHtml = string.IsNullOrWhiteSpace(model.ImportantText)
            ? string.Empty
            : $"<p class=\"campaign__text -important\"{importantStyleAttr}>{model.ImportantText}</p>";

        var imageHtml = string.IsNullOrWhiteSpace(model.Image)
            ? string.Empty
            : (IsImageDeleted(content, model.Image)
                ? string.Empty
                : $"<img class=\"campaign__iphoneImg\" src=\"{ResolveImageUrl(model.Image, template, overrides, embedImages)}\" alt=\"{WebUtility.HtmlEncode(model.ImageAlt ?? string.Empty)}\" />");

        var decoHtml = string.IsNullOrWhiteSpace(model.DecoImage)
            ? string.Empty
            : (IsImageDeleted(content, model.DecoImage)
                ? string.Empty
                : $"<div class=\"deco-img -right -deco06\" data-autofit=\"true\"><img src=\"{ResolveImageUrl(model.DecoImage, template, overrides, embedImages)}\" alt=\"{WebUtility.HtmlEncode(model.DecoAlt ?? string.Empty)}\" /></div>");

                return $@"
<div class=""campaign__inner"">
    {textHtml}
    {importantHtml}
    {imageHtml}
</div>
{decoHtml}";
    }

    private static string BuildCouponFlowSectionHtml(
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var model = content.Sections.CouponFlow;
        var title = WebUtility.HtmlEncode(model.Title ?? string.Empty);
        var lead = model.Lead ?? string.Empty;
        var note = WebUtility.HtmlEncode(model.Note ?? string.Empty);
        var buttonLabel = WebUtility.HtmlEncode(model.ButtonLabel ?? string.Empty);
        var buttonUrl = WebUtility.HtmlEncode(model.ButtonUrl ?? string.Empty);
        var accent = SanitizeCssColor(ResolveFrameStyle(content, "couponflow").HeaderBackgroundColor) ?? "#ea5504";

        var slides = model.Slides.Count > 0
            ? model.Slides
            : GetDefaultCouponFlowSlides();

        var slideHtml = string.Join(string.Empty, slides.Select((slide, index) =>
        {
            if (string.IsNullOrWhiteSpace(slide.Image) || IsImageDeleted(content, slide.Image))
            {
                return string.Empty;
            }

            var src = ResolveImageUrl(slide.Image, template, overrides, embedImages);
            var alt = WebUtility.HtmlEncode(slide.Alt ?? string.Empty);
            var active = index == 0 ? " is-active" : string.Empty;
            return $"<div class=\"swiper-slide{active}\"><img src=\"{src}\" alt=\"{alt}\" /></div>";
        }));

        if (string.IsNullOrWhiteSpace(slideHtml))
        {
            slideHtml = "<div class=\"swiper-slide is-active\"><div class=\"coupon-flow-empty\">スライド画像が未設定です</div></div>";
        }

        var bulletCount = slides.Count > 0 ? slides.Count : 1;
        var bullets = string.Join(string.Empty, Enumerable.Range(0, bulletCount).Select(index =>
        {
            var active = index == 0 ? " swiper-pagination-bullet-active" : string.Empty;
            return $"<span class=\"swiper-pagination-bullet{active}\" data-index=\"{index}\" aria-label=\"Go to slide {index + 1}\" role=\"button\"></span>";
        }));

        var items = model.Items.Count > 0
            ? model.Items
            : GetDefaultCouponFlowNotes();
        var notesHtml = string.Join(string.Empty, items.Select(item => $"<li class=\"indent\">{ToStyledHtml(item)}</li>"));

        var buttonHtml = string.IsNullOrWhiteSpace(buttonLabel)
            ? string.Empty
            : $"<div class=\"c-btn -border -l\"><a href=\"{buttonUrl}\" target=\"_blank\" rel=\"noopener noreferrer\"><span>{buttonLabel}</span></a></div>";

                return $@"
            <div class=""coupon-howto-inner"" style=""--coupon-flow-accent:{accent};"">
    {(string.IsNullOrWhiteSpace(lead) ? string.Empty : $"<div class=\"coupon-howto-lead\"><p><strong>{lead}</strong></p></div>")}
    <div class=""coupon-howto-swiper swiper coupon-flow-swiper"">
        <div class=""swiper-wrapper"">
            {slideHtml}
        </div>
        <button type=""button"" class=""swiper-button-prev"" aria-label=""前のスライド""></button>
        <button type=""button"" class=""swiper-button-next"" aria-label=""次のスライド""></button>
    </div>
    <div class=""swiper-pagination"">{bullets}</div>
    {(string.IsNullOrWhiteSpace(note) ? string.Empty : $"<p class=\"note\">{note}</p>")}
    {buttonHtml}
    {(string.IsNullOrWhiteSpace(notesHtml) ? string.Empty : $"<ul>{notesHtml}</ul>")}
</div>";
    }

    private static List<CouponFlowSlideModel> GetDefaultCouponFlowSlides()
    {
        return new List<CouponFlowSlideModel>
        {
            new() { Image = "images/slide-img1.png", Alt = "1. au PAY アプリのホームの下のクーポンから獲得できます。「一覧」でクーポン画面に遷移します。" },
            new() { Image = "images/slide-img2.png", Alt = "2. クーポン画面で、クーポンを獲得できる他、クーポンをタップするとクーポンの詳細画面を確認することができます。" },
            new() { Image = "images/slide-img3.png", Alt = "3. クーポンの詳細画面でも獲得できます。" },
            new() { Image = "images/slide-img4.png", Alt = "4. マイクーポンが自動で適応されます。" },
            new() { Image = "images/slide-img5.png", Alt = "5. クーポン対象のお店で決済します。" },
            new() { Image = "images/slide-img6.png", Alt = "6. マイクーポンが自動で適応されます。" }
        };
    }

    private static List<TextItemModel> GetDefaultCouponFlowNotes()
    {
        return new List<TextItemModel>
        {
            new() { Text = "※スマートフォンもしくはタブレットからご確認ください" },
            new() { Text = "※上記「クーポンを獲得する」ボタンからアプリへ遷移できます。" },
            new() { Text = "※クーポンは、1月1日(木・祝)から獲得できます。" }
        };
    }

    private static string BuildPaymentHistoryTextStyle(PaymentHistorySectionModel model, string kind)
    {
        var style = new StringBuilder();

        var font = SanitizeFontFamily(model.FontFamily);
        if (!string.IsNullOrWhiteSpace(font))
        {
            style.Append($"font-family:{font};");
        }

        if (!string.IsNullOrWhiteSpace(model.TextAlign))
        {
            style.Append($"text-align:{model.TextAlign};");
        }

        switch (kind)
        {
            case "title":
                if (model.TitleFontSize is > 0)
                {
                    style.Append($"font-size:{model.TitleFontSize}px;");
                }
                var titleColor = SanitizeCssColor(model.TitleColor);
                if (!string.IsNullOrWhiteSpace(titleColor))
                {
                    style.Append($"color:{titleColor};");
                }
                if (model.TitleBold)
                {
                    style.Append("font-weight:700;");
                }
                break;
            case "important":
                if (model.ImportantFontSize is > 0)
                {
                    style.Append($"font-size:{model.ImportantFontSize}px;");
                }
                var importantColor = SanitizeCssColor(model.ImportantColor);
                if (!string.IsNullOrWhiteSpace(importantColor))
                {
                    style.Append($"color:{importantColor};");
                }
                if (model.ImportantBold)
                {
                    style.Append("font-weight:700;");
                }
                break;
            default:
                if (model.TextFontSize is > 0)
                {
                    style.Append($"font-size:{model.TextFontSize}px;");
                }
                var textColor = SanitizeCssColor(model.TextColor);
                if (!string.IsNullOrWhiteSpace(textColor))
                {
                    style.Append($"color:{textColor};");
                }
                if (model.TextBold)
                {
                    style.Append("font-weight:700;");
                }
                break;
        }

        return style.ToString();
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
                var ranking = content.Sections.Ranking;
                var title = string.IsNullOrWhiteSpace(ranking.Title)
                        ? ranking.TitleLines.FirstOrDefault()?.Text ?? "最新順位はこちら"
                        : ranking.Title;
                var campaignName = string.IsNullOrWhiteSpace(ranking.CampaignName)
                        ? (string.IsNullOrWhiteSpace(ranking.Subtitle) ? ranking.SubtitleLines.FirstOrDefault()?.Text ?? string.Empty : ranking.Subtitle)
                        : ranking.CampaignName;
                var periodText = ranking.PeriodText ?? string.Empty;
                var asOfText = ranking.AsOfText ?? string.Empty;

                var headerLabels = ranking.HeaderLabels
                        .Select(label => (label ?? string.Empty).Trim())
                        .ToList();
                if (headerLabels.Count == 0)
                {
                    headerLabels = new List<string> { "順位", "決済金額", "品数" };
                }
                if (headerLabels.Count < 2)
                {
                    headerLabels = new List<string> { "順位", "決済金額" };
                }
                if (headerLabels.Count > 3)
                {
                    headerLabels = headerLabels.Take(3).ToList();
                }

                var headerCells = string.Join(string.Empty, headerLabels.Select(label => $"<th>{WebUtility.HtmlEncode(label)}</th>"));

                var rows = ranking.Rows.Count == 0
                        ? new List<RankingRowModel>
                        {
                                new() { Rank = "1", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "2", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "3", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "4", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "10", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "100", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "300", Amount = "000,000円", Items = "000品以上" },
                                new() { Rank = "1,000", Amount = "000,000円", Items = "000品以上" }
                        }
                        : ranking.Rows;

                var bodyRows = string.Join(string.Empty, rows.Select((row, rowIndex) =>
                {
                    var isTop = ranking.ShowCrowns && rowIndex < 3;
                    var kingClass = isTop ? $"king king-0{rowIndex + 1} -number" : "-number";
                    var cells = new List<string>
                    {
                        $"<td class=\"{kingClass}\">{WebUtility.HtmlEncode(row.Rank)}</td>",
                        $"<td>{WebUtility.HtmlEncode(row.Amount)}</td>"
                    };
                    if (headerLabels.Count >= 3)
                    {
                        cells.Add($"<td>{WebUtility.HtmlEncode(row.Items)}</td>");
                    }
                    return $"<tr>{string.Join(string.Empty, cells)}</tr>";
                }));

                var notes = ranking.NotesItems.Count > 0
                        ? ranking.NotesItems.Where(item => item.Visible && !string.IsNullOrWhiteSpace(item.Text)).Select(item => item.Text)
                        : SplitLines(ranking.TableNotes);
                var notesHtml = notes.Any()
                        ? $"<ul class=\"c-notes-list campaign__rank-notes\">{string.Join(string.Empty, notes.Select(n => $"<li>{WebUtility.HtmlEncode(n)}</li>"))}</ul>"
                        : string.Empty;

                var leftImageHtml = BuildRankingDecorImageHtml(ResolveRankingDecorImage(ranking.LeftTopImage, ranking.ImageLeft), template, overrides, embedImages, "-left -deco04");
                var rightImageHtml = BuildRankingDecorImageHtml(ResolveRankingDecorImage(ranking.RightTopImage, ranking.ImageRight), template, overrides, embedImages, "-right -deco05");

                return $@"
<div class=""ranking-inner campaign__rank-block"">
    <p class=""campaign__rank-text"">
        {(string.IsNullOrWhiteSpace(campaignName) ? string.Empty : $"<span class=\"-heading\">{WebUtility.HtmlEncode(campaignName)}</span>")}
        {(string.IsNullOrWhiteSpace(periodText) ? string.Empty : $"<span class=\"-kikan\"><span>{WebUtility.HtmlEncode(ranking.PeriodLabel)}</span>{WebUtility.HtmlEncode(periodText)}</span>")}
        {(string.IsNullOrWhiteSpace(asOfText) ? string.Empty : $"<span class=\"-date\">{WebUtility.HtmlEncode(asOfText)}</span>")}
    </p>
    <table class=""campaign__rank-box"">
        <thead>
            <tr>{headerCells}</tr>
        </thead>
        <tbody>
            {bodyRows}
        </tbody>
    </table>
    {notesHtml}
    {leftImageHtml}
    {rightImageHtml}
</div>";
        }

    private static IEnumerable<string> SplitLines(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Enumerable.Empty<string>();
        }

        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));
    }

    private static RankingDecorImageModel ResolveRankingDecorImage(RankingDecorImageModel image, string fallbackPath)
    {
        if (!string.IsNullOrWhiteSpace(image.Src))
        {
            return image;
        }

        return new RankingDecorImageModel
        {
            Src = fallbackPath,
            Alt = string.Empty,
            Visible = true
        };
    }

    private static string BuildRankingDecorImageHtml(
        RankingDecorImageModel image,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages,
        string className)
    {
        if (image is null || !image.Visible || string.IsNullOrWhiteSpace(image.Src))
        {
            return string.Empty;
        }

        var src = ResolveImageUrl(image.Src, template, overrides, embedImages);
        var alt = WebUtility.HtmlEncode(image.Alt ?? string.Empty);
        return $"<div class=\"deco-img {className}\"><img src=\"{src}\" alt=\"{alt}\" /></div>";
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
        var noticeTitleValue = string.IsNullOrWhiteSpace(content.Sections.StoreSearch.NoticeTitle)
            ? "⚠️ ご注意ください！"
            : content.Sections.StoreSearch.NoticeTitle;
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
</div>";
	}

	private static string BuildCustomSectionHtml(
		CustomSectionModel section,
		ContentModel content,
		TemplateProject template,
		IDictionary<string, byte[]>? overrides,
		bool embedImages)
	{
		var bodyHtml = RenderTextLines(section.BodyTextItems, "custom-section-body-lines");
		var notesHtml = RenderTextLines(section.ImageNotesItems, "custom-section-notes");
        var imageHtml = BuildCustomSectionGalleryHtml(section, content, template, overrides, embedImages);
		var linkHtml = string.Empty;
		if (!string.IsNullOrWhiteSpace(section.LinkUrl) && IsValidUrl(section.LinkUrl))
		{
			var url = WebUtility.HtmlEncode(section.LinkUrl);
			linkHtml = $"<div class=\"custom-section-link\"><a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">詳細を見る</a></div>";
		}

		return $@"
<div class=""custom-section-body"">
    {bodyHtml}
    {imageHtml}
    {notesHtml}
    {linkHtml}
</div>";
	}

    private static string BuildCustomSectionGalleryHtml(
        CustomSectionModel section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var images = section.Images
            .Where(img => img is not null && !string.IsNullOrWhiteSpace(img.Src))
            .Where(img => !IsImageDeleted(content, img.Src))
            .ToList();

        if (images.Count == 0)
        {
            return BuildLegacyCustomImageHtml(section, content, template, overrides, embedImages);
        }

        var mode = NormalizeGalleryMode(section.GalleryMode);
        return mode switch
        {
            "carousel" => BuildCustomCarouselHtml(section, images, template, overrides, embedImages),
            "grid" => BuildCustomGridHtml(section, images, template, overrides, embedImages),
            _ => BuildCustomSingleHtml(section, images, template, overrides, embedImages)
        };
    }

    private static string BuildLegacyCustomImageHtml(
        CustomSectionModel section,
        ContentModel content,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        if (string.IsNullOrWhiteSpace(section.ImagePath) || IsImageDeleted(content, section.ImagePath))
        {
            return string.Empty;
        }

        var imgSrc = ResolveImageUrl(section.ImagePath, template, overrides, embedImages);
        var alt = WebUtility.HtmlEncode(section.ImageAlt ?? string.Empty);
        return $"<div class=\"custom-section-image\"><img src=\"{imgSrc}\" alt=\"{alt}\" /></div>";
    }

    private static string BuildCustomSingleHtml(
        CustomSectionModel section,
        List<CustomSectionImageModel> images,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var first = images.First();
        var imageTag = BuildCustomImageTag(section, first, template, overrides, embedImages);
        return string.IsNullOrWhiteSpace(imageTag)
            ? string.Empty
            : $"<div class=\"custom-section-image\">{imageTag}</div>";
    }

    private static string BuildCustomCarouselHtml(
        CustomSectionModel section,
        List<CustomSectionImageModel> images,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var slides = string.Join("", images.Select(img => $"<div class=\"custom-gallery-slide\">{BuildCustomImageTag(section, img, template, overrides, embedImages)}</div>"));
        var showArrows = section.GalleryShowArrows ? "true" : "false";
        var showDots = section.GalleryShowDots ? "true" : "false";
        var loop = section.GalleryLoop ? "true" : "false";
        var autoplay = section.GalleryAutoplay ? "true" : "false";
        var interval = Math.Max(1000, section.GalleryIntervalMs);

                return $@"<div class=""custom-gallery custom-gallery--carousel"" data-carousel=""true"" data-show-arrows=""{showArrows}"" data-show-dots=""{showDots}"" data-loop=""{loop}"" data-autoplay=""{autoplay}"" data-interval=""{interval}"">
    <div class=""custom-gallery-track"">{slides}</div>
    <button type=""button"" class=""custom-gallery-nav custom-gallery-prev"" aria-label=""前へ"">‹</button>
    <button type=""button"" class=""custom-gallery-nav custom-gallery-next"" aria-label=""次へ"">›</button>
    <div class=""custom-gallery-dots""></div>
</div>";
    }

    private static string BuildCustomGridHtml(
        CustomSectionModel section,
        List<CustomSectionImageModel> images,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        var items = string.Join("", images.Select(img => $"<div class=\"custom-gallery-item\">{BuildCustomImageTag(section, img, template, overrides, embedImages)}</div>"));
        return $"<div class=\"custom-gallery custom-gallery--grid\">{items}</div>";
    }

    private static string BuildCustomImageTag(
        CustomSectionModel section,
        CustomSectionImageModel image,
        TemplateProject template,
        IDictionary<string, byte[]>? overrides,
        bool embedImages)
    {
        if (string.IsNullOrWhiteSpace(image.Src))
        {
            return string.Empty;
        }

        var src = ResolveImageUrl(image.Src, template, overrides, embedImages);
        var altValue = string.IsNullOrWhiteSpace(image.Alt) ? section.ImageAlt : image.Alt;
        var alt = WebUtility.HtmlEncode(altValue ?? string.Empty);
        var imgTag = $"<img src=\"{src}\" alt=\"{alt}\" />";
        if (!string.IsNullOrWhiteSpace(image.Link) && IsValidUrl(image.Link))
        {
            var url = WebUtility.HtmlEncode(image.Link);
            return $"<a class=\"custom-gallery-link\" href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{imgTag}</a>";
        }

        return imgTag;
    }

    private static string NormalizeGalleryMode(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? "single" : mode.Trim().ToLowerInvariant();
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
        var stateList = content.SectionGroups?.Where(group => !string.IsNullOrWhiteSpace(group.Key)).ToList()
            ?? new List<SectionGroupModel>();
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

        foreach (var element in document.QuerySelectorAll(".section-group[data-section]").ToList())
        {
            var key = element.GetAttribute("data-section") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }
            if (!IsEditorManagedSectionKey(key, content))
            {
                continue;
            }
            if (!enabledMap.ContainsKey(key)
                || (enabledMap.TryGetValue(key, out var enabled) && !enabled))
            {
                element.Remove();
            }
        }

        if (stateList.Count == 0)
        {
            return;
        }

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
            "couponflow" => true,
            "stickytabs" => true,
            "couponnotes" => true,
            "ranking" => true,
            "paymenthistory" => true,
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
            HideCountdownElements(document, dateRangeText);
            return;
        }

        foreach (var element in document.QuerySelectorAll("[data-countdown-hidden='true']").ToList())
        {
            element.RemoveAttribute("data-countdown-hidden");
        }

        UpdateCountdownPeriodText(document, dateRangeText);
    }

    private static void HideCountdownElements(IDocument document, string dateRangeText)
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

    private static void EnsurePaymentHistoryStyle(IDocument document, ContentModel content)
    {
        if (!content.Sections.PaymentHistory.Enabled)
        {
            return;
        }

        var head = document.Head;
        if (head is null || document.QuerySelector("style[data-payment-history-style='true']") is not null)
        {
            return;
        }

        var style = document.CreateElement("style");
        style.SetAttribute("data-payment-history-style", "true");
        style.TextContent = @"
    .payment-history-section .campaign__text.-important { margin-top: 30px; }
    .payment-history-section .campaign__iphoneImg { margin: 25px auto 0; width: 420px; max-width: 100%; display: block; }
    @media (max-width: 767px) {
      .payment-history-section .campaign__text.-important { margin-top: 15px; }
    }
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
