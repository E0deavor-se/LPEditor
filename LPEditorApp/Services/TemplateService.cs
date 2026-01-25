using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using LPEditorApp.Models;
using LPEditorApp.Utils;
using ILogger = LPEditorApp.Utils.ILogger;

namespace LPEditorApp.Services;

public class TemplateService
{
    private readonly ILogger _logger;

    public TemplateService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<TemplateProject> LoadTemplateAsync(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
        {
            throw new TemplateException($"テンプレートZIPが見つかりません: {zipFilePath}");
        }

        var template = new TemplateProject { SourceZipPath = zipFilePath };

        try
        {
            using var zipStream = File.OpenRead(zipFilePath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var rootPrefix = DetectRootPrefix(archive);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FullName))
                {
                    continue;
                }

                if (TemplateProject.IsExcludedPath(entry.FullName))
                {
                    continue;
                }

                if (entry.FullName.EndsWith("/"))
                {
                    continue;
                }

                var normalizedPath = entry.FullName.Replace("\\", "/");
                if (!string.IsNullOrWhiteSpace(rootPrefix) && normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedPath = normalizedPath.Substring(rootPrefix.Length);
                }

                if (string.IsNullOrWhiteSpace(normalizedPath) || normalizedPath.EndsWith("/"))
                {
                    continue;
                }

                var fileInfo = new TemplateFileInfo
                {
                    RelativePath = normalizedPath
                };

                DetectFileType(fileInfo);

                await using var stream = entry.Open();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                fileInfo.Data = ms.ToArray();

                if (fileInfo.Data.Length == 0 && entry.Length > 0)
                {
                    _logger.Warn($"テンプレート読込: データが0バイトです: {fileInfo.RelativePath} (entryLength={entry.Length})");
                }

                template.Files[fileInfo.RelativePath] = fileInfo;
            }

            if (template.Files.TryGetValue("content.json", out var contentFile))
            {
                try
                {
                    var json = System.Text.Encoding.UTF8.GetString(contentFile.Data);
                    template.CurrentContent = JsonSerializer.Deserialize<ContentModel>(json) ?? new ContentModel();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"content.jsonの読み込みに失敗: {ex.Message}");
                    template.CurrentContent = new ContentModel();
                }
            }
            else
            {
                template.CurrentContent = new ContentModel();
            }

            try
            {
                EnsureSectionOrder(template.CurrentContent);
            }
            catch (Exception ex)
            {
                _logger.Warn($"セクション順の整形に失敗: {ex.Message}");
            }

            try
            {
                await ApplyHtmlDefaultsAsync(template, template.CurrentContent);
            }
            catch (Exception ex)
            {
                _logger.Warn($"HTML既定値の解析に失敗: {ex.Message}");
            }

            _logger.Info($"テンプレート読み込み完了: {template.Files.Count} 件");
            return template;
        }
        catch (Exception ex)
        {
            _logger.Error($"テンプレート読み込み失敗: {ex.Message}");
            throw new TemplateException($"ZIP読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    private static string? DetectRootPrefix(ZipArchive archive)
    {
        var entryPaths = archive.Entries
            .Select(entry => entry.FullName.Replace("\\", "/"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (entryPaths.Count == 0)
        {
            return null;
        }

        var firstPath = entryPaths[0];
        var firstSlash = firstPath.IndexOf('/');
        if (firstSlash <= 0)
        {
            return null;
        }

        var candidate = firstPath.Substring(0, firstSlash + 1);
        var isSingleRoot = entryPaths.All(path => path.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
        return isSingleRoot ? candidate : null;
    }

    private static void DetectFileType(TemplateFileInfo fileInfo)
    {
        var ext = Path.GetExtension(fileInfo.RelativePath).ToLowerInvariant();
        fileInfo.IsImage = ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" or ".bmp";
        fileInfo.IsHtml = ext == ".html";
        fileInfo.IsJs = ext == ".js";
        fileInfo.IsCss = ext == ".css";
        fileInfo.IsJson = ext == ".json";
    }

    private static void EnsureSectionOrder(ContentModel content)
    {
        var available = new List<string> { "campaignContent", "couponPeriod", "storeSearch", "couponNotes", "ranking", "countdown" };
        if (content.SectionsOrder is null || content.SectionsOrder.Count == 0)
        {
            content.SectionsOrder = new List<string>(available);
            return;
        }

        content.SectionsOrder = content.SectionsOrder
            .Where(value => available.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var item in available)
        {
            if (!content.SectionsOrder.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                content.SectionsOrder.Add(item);
            }
        }
    }

    private static async Task ApplyHtmlDefaultsAsync(TemplateProject template, ContentModel content)
    {
        var indexFile = template.Files.GetValueOrDefault("index.html")
            ?? template.Files.FirstOrDefault(pair => pair.Key.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase)).Value;

        if (indexFile is null)
        {
            return;
        }

        var html = System.Text.Encoding.UTF8.GetString(indexFile.Data);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        if (IsEmpty(content.Meta.PageTitle))
        {
            content.Meta.PageTitle = document.Title?.Trim() ?? string.Empty;
        }

        if (IsEmpty(content.Meta.Description))
        {
            content.Meta.Description = document.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim() ?? string.Empty;
        }

        if (IsEmpty(content.Header.LogoAlt))
        {
            content.Header.LogoAlt = document.QuerySelector(".header-logo__img img, .footer-logo__img img")?.GetAttribute("alt")?.Trim() ?? string.Empty;
        }

        if (IsEmpty(content.Hero.Alt))
        {
            content.Hero.Alt = document.QuerySelector(".mv picture img, .mv img")?.GetAttribute("alt")?.Trim() ?? string.Empty;
        }

        var sectionGroupKeys = ExtractSectionGroupKeys(document);
        if (sectionGroupKeys.Count > 0)
        {
            template.SectionGroupKeys = sectionGroupKeys;
            EnsureSectionGroups(content, sectionGroupKeys);
        }

        var campaignContentSection = document.QuerySelector(".section-group[data-section='campaign-content']")
            ?? document.QuerySelector(".section-group[data-section='campaignContent']");
        var couponNotesSection = document.QuerySelector(".section-group[data-section='coupon-notes']")
            ?? document.QuerySelector(".section-group[data-section='couponNotes']");
        var couponPeriodSection = document.QuerySelector(".section-group[data-section='coupon-period']")
            ?? document.QuerySelector(".section-group[data-section='couponPeriod']");
        var storeSearchSection = document.QuerySelector(".section-group[data-section='store-search']")
            ?? document.QuerySelector(".section-group[data-section='storeSearch']");

        if (campaignContentSection is not null)
        {
            if (IsEmpty(content.Sections.CampaignContent.Title))
            {
                content.Sections.CampaignContent.Title = GetText(campaignContentSection, ".campaign__heading");
            }

            if (IsEmpty(content.Sections.CampaignContent.Body))
            {
                content.Sections.CampaignContent.Body = GetMultilineText(campaignContentSection, ".campaign__text");
            }

            if (content.Sections.CampaignContent.Notes.Count == 0)
            {
                content.Sections.CampaignContent.Notes = GetListItems(campaignContentSection, ".campaign__subBox .c-list");
            }
        }

        if (couponNotesSection is not null)
        {
            if (IsEmpty(content.Sections.CouponNotes.Title))
            {
                content.Sections.CouponNotes.Title = GetText(couponNotesSection, ".campaign__heading");
            }

            if (content.Sections.CouponNotes.Items.Count == 0)
            {
                content.Sections.CouponNotes.Items = GetListItems(couponNotesSection, ".c-list");
            }
        }

        if (couponPeriodSection is not null)
        {
            if (IsEmpty(content.Sections.CouponPeriod.Title))
            {
                content.Sections.CouponPeriod.Title = GetText(couponPeriodSection, ".campaign__heading");
            }

            if (IsEmpty(content.Sections.CouponPeriod.Text))
            {
                content.Sections.CouponPeriod.Text = GetMultilineText(couponPeriodSection, ".campaign__text");
            }
        }

        if (storeSearchSection is not null)
        {
            if (IsEmpty(content.Sections.StoreSearch.Title))
            {
                content.Sections.StoreSearch.Title = GetText(storeSearchSection, ".campaign__heading");
            }

            if (IsEmpty(content.Sections.StoreSearch.NoticeTitle))
            {
                content.Sections.StoreSearch.NoticeTitle = GetText(storeSearchSection, ".store-search-notice-heading");
            }

            if (content.Sections.StoreSearch.NoticeItems.Count == 0)
            {
                content.Sections.StoreSearch.NoticeItems = GetListItems(storeSearchSection, ".store-search-notice-list");
            }
        }

        var conditionsSection = document.QuerySelector("section.conditions");
        if (conditionsSection is not null)
        {
            if (IsEmpty(content.Sections.Conditions.DeviceText))
            {
                content.Sections.Conditions.DeviceText = GetText(conditionsSection, ".conditions__model");
            }

            if (content.Sections.Conditions.Items.Count == 0)
            {
                content.Sections.Conditions.Items = GetListItems(conditionsSection, ".conditionsNotesBox__notes");
            }
        }

        var contactSection = document.QuerySelector("section.contact");
        if (contactSection is not null)
        {
            if (IsEmpty(content.Sections.Contact.Title))
            {
                content.Sections.Contact.Title = GetMultilineText(contactSection, ".contact__title");
            }

            if (IsEmpty(content.Sections.Contact.Lead))
            {
                content.Sections.Contact.Lead = GetMultilineText(contactSection, ".contact__lead");
            }

            if (content.Sections.Contact.Buttons.Count == 0)
            {
                var buttons = new List<ButtonItemModel>();
                foreach (var anchor in contactSection.QuerySelectorAll(".contact__item a"))
                {
                    var label = GetText(anchor);
                    var url = anchor.GetAttribute("href") ?? string.Empty;
                    var emphasis = anchor.QuerySelector(".is-emphasis") is not null || anchor.ClassList.Contains("is-emphasis");
                    buttons.Add(new ButtonItemModel { Label = label, Url = url, Emphasis = emphasis });
                }

                content.Sections.Contact.Buttons = buttons;
            }

            if (IsEmpty(content.Sections.Contact.OfficeHours))
            {
                content.Sections.Contact.OfficeHours = GetMultilineText(contactSection, ".contact__text");
            }
        }

        if (IsEmpty(content.Sections.Banners.Main.Url))
        {
            content.Sections.Banners.Main.Url = document.QuerySelector("a.banner")?.GetAttribute("href") ?? string.Empty;
        }

        if (IsEmpty(content.Sections.Banners.Main.Image))
        {
            content.Sections.Banners.Main.Image = document.QuerySelector("a.banner .banner__main img")?.GetAttribute("src") ?? string.Empty;
        }

        if (IsEmpty(content.Sections.Banners.Magazine.Url))
        {
            content.Sections.Banners.Magazine.Url = document.QuerySelector(".magazine__btn")?.GetAttribute("href") ?? string.Empty;
        }

        if (IsEmpty(content.Sections.Banners.Magazine.Image))
        {
            content.Sections.Banners.Magazine.Image = document.QuerySelector(".magazine__btn img")?.GetAttribute("src") ?? string.Empty;
        }
    }

    public static void EnsureSectionGroups(ContentModel content, IEnumerable<string> keys)
    {
        var normalized = keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var editorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "campaign-content",
            "campaignContent",
            "coupon-period",
            "couponPeriod",
            "store-search",
            "storeSearch",
            "coupon-notes",
            "couponNotes",
            "ranking",
            "countdown"
        };

        if (content.CustomSections is not null && content.CustomSections.Count > 0)
        {
            foreach (var custom in content.CustomSections)
            {
                if (!string.IsNullOrWhiteSpace(custom.Key))
                {
                    editorKeys.Add(custom.Key.Trim());
                }
            }
        }

        normalized = normalized
            .Where(key => editorKeys.Contains(key))
            .ToList();

        if (normalized.Count == 0)
        {
            return;
        }

        if (content.SectionGroups is null || content.SectionGroups.Count == 0)
        {
            content.SectionGroups = normalized
                .Select(key => new SectionGroupModel { Key = key, Enabled = true })
                .ToList();
            return;
        }

        var indexMap = normalized
            .Select((key, index) => new { key, index })
            .ToDictionary(item => item.key, item => item.index, StringComparer.OrdinalIgnoreCase);

        var existing = content.SectionGroups
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToList();

        var ordered = existing
            .Where(group => indexMap.ContainsKey(group.Key))
            .OrderBy(group => indexMap[group.Key])
            .ToList();

        var extras = existing
            .Where(group => !indexMap.ContainsKey(group.Key) && editorKeys.Contains(group.Key))
            .ToList();

        var existingKeys = new HashSet<string>(ordered.Select(group => group.Key), StringComparer.OrdinalIgnoreCase);
        foreach (var key in normalized)
        {
            if (!existingKeys.Contains(key))
            {
                ordered.Add(new SectionGroupModel { Key = key, Enabled = true });
            }
        }

        foreach (var extra in extras)
        {
            if (!ordered.Any(group => string.Equals(group.Key, extra.Key, StringComparison.OrdinalIgnoreCase)))
            {
                ordered.Add(extra);
            }
        }

        content.SectionGroups = ordered;
    }

    private static List<string> ExtractSectionGroupKeys(IDocument document)
    {
        return document.QuerySelectorAll(".section-group[data-section]")
            .Select(element => element.GetAttribute("data-section")?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? TryGetInlineStyleColor(IDocument document, string selector, string propertyName)
    {
        var element = document.QuerySelector(selector);
        if (element is null)
        {
            return null;
        }

        var style = element.GetAttribute("style");
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        return NormalizeColor(ExtractCssPropertyValue(style, propertyName));
    }

    private static string? ExtractCssColor(IEnumerable<string> cssSources, string selectorToken, string propertyName)
    {
        foreach (var cssText in cssSources)
        {
            var value = FindCssPropertyValue(cssText, selectorToken, propertyName);
            var normalized = NormalizeColor(value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static string? FindCssPropertyValue(string cssText, string selectorToken, string propertyName)
    {
        var ruleMatches = Regex.Matches(cssText, "(?<selector>[^{}]+)\\{(?<body>[^{}]+)\\}", RegexOptions.Singleline);
        foreach (Match match in ruleMatches)
        {
            var selector = match.Groups["selector"].Value;
            if (string.IsNullOrWhiteSpace(selector) || !selector.Contains(selectorToken, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var body = match.Groups["body"].Value;
            var value = ExtractCssPropertyValue(body, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ExtractCssPropertyValue(string cssBody, string propertyName)
    {
        var match = Regex.Match(cssBody, $"{Regex.Escape(propertyName)}\\s*:\\s*(?<value>[^;]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static bool IsEmpty(string? value)
        => string.IsNullOrWhiteSpace(value);

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("#", StringComparison.OrdinalIgnoreCase))
        {
            var hex = trimmed.Substring(1);
            if (hex.Length == 3)
            {
                return "#" + string.Concat(hex.Select(ch => new string(ch, 2)));
            }

            if (hex.Length >= 6)
            {
                return "#" + hex.Substring(0, 6);
            }

            return null;
        }

        var rgbMatch = Regex.Match(trimmed, "^rgba?\\((?<r>\\d{1,3})\\s*,\\s*(?<g>\\d{1,3})\\s*,\\s*(?<b>\\d{1,3})", RegexOptions.IgnoreCase);
        if (rgbMatch.Success)
        {
            var r = ClampByte(rgbMatch.Groups["r"].Value);
            var g = ClampByte(rgbMatch.Groups["g"].Value);
            var b = ClampByte(rgbMatch.Groups["b"].Value);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        return null;
    }

    private static int ClampByte(string value)
    {
        if (!int.TryParse(value, out var parsed))
        {
            return 0;
        }

        return Math.Clamp(parsed, 0, 255);
    }

    private static string? ResolveTemplatePath(TemplateProject template, string href, string baseDir)
    {
        var normalized = NormalizePath(href);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(1);
        }
        else if (!string.IsNullOrWhiteSpace(baseDir))
        {
            normalized = NormalizePath(baseDir + normalized);
        }

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

    private static string NormalizePath(string path)
        => path.Replace("\\", "/").Trim();

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

    private static string GetText(IParentNode root, string selector)
        => root.QuerySelector(selector)?.TextContent.Trim() ?? string.Empty;

    private static string GetText(IElement element)
        => element.TextContent.Trim();

    private static string GetMultilineText(IParentNode root, string selector)
    {
        var node = root.QuerySelector(selector);
        if (node is null)
        {
            return string.Empty;
        }

        var html = node.InnerHtml;
        var normalized = html.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);

        return Regex.Replace(normalized, "<.*?>", string.Empty).Trim();
    }

    private static List<TextItemModel> GetListItems(IParentNode root, string selector)
    {
        var list = new List<TextItemModel>();
        var target = root.QuerySelector(selector);
        if (target is null)
        {
            return list;
        }

        foreach (var li in target.QuerySelectorAll("li"))
        {
            var emphasis = li.QuerySelector(".is-emphasis") is not null;
            var text = li.TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                list.Add(new TextItemModel { Text = text, Emphasis = emphasis });
            }
        }

        return list;
    }
}
