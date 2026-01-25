using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using LPEditorApp.Models;
using LPEditorApp.Utils;
using ILogger = LPEditorApp.Utils.ILogger;

namespace LPEditorApp.Services;

public class LpImportService
{
    private static readonly HashSet<string> TextTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1", "h2", "h3", "p", "li", "span"
    };

    private static readonly HashSet<string> FrozenTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "iframe", "canvas", "video", "script"
    };

    private readonly ILogger _logger;

    public LpImportService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<LpImportSession> LoadZipAsync(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
        {
            throw new InvalidOperationException($"ZIPが見つかりません: {zipFilePath}");
        }

        var session = new LpImportSession
        {
            SourceZipPath = zipFilePath
        };

        using var zipStream = File.OpenRead(zipFilePath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var rootPrefix = DetectRootPrefix(archive);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            if (entry.FullName.EndsWith("/"))
            {
                continue;
            }

            var normalizedPath = entry.FullName.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(rootPrefix)
                && normalizedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath[rootPrefix.Length..];
            }

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                continue;
            }

            await using var stream = entry.Open();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            session.Files[normalizedPath] = new ImportedFile
            {
                RelativePath = normalizedPath,
                Data = ms.ToArray()
            };

            if (IsHtmlFile(normalizedPath))
            {
                session.HtmlEntries.Add(normalizedPath);
            }
        }

        session.HtmlEntries.Sort(StringComparer.OrdinalIgnoreCase);
        return session;
    }

    public async Task<LpImportProject> AnalyzeAsync(LpImportSession session, string entryHtmlPath)
    {
        if (!session.Files.TryGetValue(entryHtmlPath, out var entry))
        {
            throw new InvalidOperationException("指定したHTMLがZIP内に見つかりません。");
        }

        var html = Encoding.UTF8.GetString(entry.Data);
        var document = await CreateDocumentAsync(html);
        var project = new LpImportProject
        {
            SourceZipPath = session.SourceZipPath,
            EntryHtmlPath = entryHtmlPath,
            EntryHtmlOriginal = html
        };

        foreach (var pair in session.Files)
        {
            project.Files[pair.Key] = pair.Value;
        }

        ExtractAssets(document, entryHtmlPath, project, session.Files);
        ExtractSections(document, project);
        project.Stats = BuildStats(project);
        project.IsReady = true;

        return project;
    }

    public async Task<string> BuildPreviewHtmlAsync(LpImportProject project)
    {
        var document = await CreateDocumentAsync(project.EntryHtmlOriginal);
        ApplyEditsToDocument(document, project, logFailures: false);
        InlineLocalAssetsForPreview(document, project);
        return document.DocumentElement?.OuterHtml ?? project.EntryHtmlOriginal;
    }

    public async Task<string> BuildOutputHtmlAsync(LpImportProject project, bool logFailures)
    {
        var document = await CreateDocumentAsync(project.EntryHtmlOriginal);
        ApplyEditsToDocument(document, project, logFailures);
        RemoveTempAttributes(document);
        return document.DocumentElement?.OuterHtml ?? project.EntryHtmlOriginal;
    }

    public async Task ExportAsync(LpImportProject project, string outputZipPath)
    {
        if (string.IsNullOrWhiteSpace(outputZipPath))
        {
            throw new InvalidOperationException("出力ZIPパスが未設定です。");
        }

        if (File.Exists(outputZipPath))
        {
            File.Delete(outputZipPath);
        }

        var html = await BuildOutputHtmlAsync(project, logFailures: true);

        await using var zipStream = File.Create(outputZipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        foreach (var file in project.Files.Values)
        {
            var path = NormalizePath(file.RelativePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (path.Equals(project.EntryHtmlPath, StringComparison.OrdinalIgnoreCase))
            {
                AddTextEntry(archive, path, html);
                continue;
            }

            if (project.AssetOverrides.TryGetValue(path, out var overrideBytes))
            {
                AddBinaryEntry(archive, path, overrideBytes);
                continue;
            }

            AddBinaryEntry(archive, path, file.Data);
        }

        foreach (var newAsset in project.NewAssets)
        {
            AddBinaryEntry(archive, newAsset.Key, newAsset.Value);
        }
    }

    private static async Task<IDocument> CreateDocumentAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    private static void ExtractAssets(IDocument document, string entryHtmlPath, LpImportProject project, IDictionary<string, ImportedFile> files)
    {
        var baseDir = GetBaseDirectory(entryHtmlPath);
        var missingAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in document.QuerySelectorAll("link[href]"))
        {
            var href = link.GetAttribute("href") ?? string.Empty;
            TryTrackAsset(href, baseDir, files, missingAssets, project);
        }

        foreach (var script in document.QuerySelectorAll("script[src]"))
        {
            var src = script.GetAttribute("src") ?? string.Empty;
            TryTrackAsset(src, baseDir, files, missingAssets, project);
        }

        foreach (var image in document.QuerySelectorAll("img[src]"))
        {
            var src = image.GetAttribute("src") ?? string.Empty;
            TryTrackAsset(src, baseDir, files, missingAssets, project);
        }

        foreach (var style in document.QuerySelectorAll("[style]"))
        {
            var styleText = style.GetAttribute("style") ?? string.Empty;
            foreach (var url in ExtractCssUrls(styleText))
            {
                TryTrackAsset(url, baseDir, files, missingAssets, project);
            }
        }

        foreach (var styleTag in document.QuerySelectorAll("style"))
        {
            var cssText = styleTag.TextContent ?? string.Empty;
            foreach (var url in ExtractCssUrls(cssText))
            {
                TryTrackAsset(url, baseDir, files, missingAssets, project);
            }
        }

        foreach (var missing in missingAssets)
        {
            project.MissingAssets.Add(new LpImportWarning
            {
                Code = "missing-asset",
                Message = "参照アセットがZIP内に見つかりません",
                Detail = missing
            });
        }
    }

    private static void TryTrackAsset(string rawPath, string baseDir, IDictionary<string, ImportedFile> files, HashSet<string> missingAssets, LpImportProject project)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return;
        }

        if (IsExternalUrl(rawPath))
        {
            return;
        }

        var resolved = ResolveRelativePath(baseDir, rawPath);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return;
        }

        if (!files.ContainsKey(resolved))
        {
            missingAssets.Add(resolved);
            return;
        }

        project.Warnings.Add(new LpImportWarning
        {
            Code = "asset-ok",
            Message = "アセットを検出",
            Detail = resolved
        });
    }

    private static void ExtractSections(IDocument document, LpImportProject project)
    {
        var sections = document.QuerySelectorAll("section").ToList();
        if (sections.Count == 0 && document.Body is not null)
        {
            sections = document.Body.Children.ToList();
        }

        var index = 1;
        foreach (var section in sections)
        {
            var model = new LpImportSection
            {
                Id = $"import-section-{index}",
                Title = GetSectionTitle(section, index),
                NodePath = BuildNodePath(section, document.Body),
                SelectorHint = BuildSelectorHint(section)
            };

            var frozenReasons = DetectFrozenReasons(section);
            foreach (var reason in frozenReasons)
            {
                model.FrozenReasons.Add(reason);
            }

            ExtractBlocks(section, model, document.Body, project);
            project.Sections.Add(model);
            index++;
        }

        if (project.Sections.Count == 0)
        {
            project.Sections.Add(new LpImportSection
            {
                Id = "import-section-1",
                Title = "セクション 1",
                FrozenReasons = { "section-not-found" }
            });
        }
    }

    private static void ExtractBlocks(IElement section, LpImportSection model, IElement? root, LpImportProject project)
    {
        var blockIndex = 1;
        foreach (var element in section.QuerySelectorAll("h1,h2,h3,p,li,span"))
        {
            if (!TextTags.Contains(element.TagName))
            {
                continue;
            }

            if (element.Closest("a") is not null)
            {
                continue;
            }

            var text = element.TextContent?.Trim() ?? string.Empty;
            if (text.Length < 4)
            {
                continue;
            }

            model.Blocks.Add(new LpImportBlock
            {
                BlockId = $"{model.Id}-text-{blockIndex++}",
                Type = LpImportBlockType.Text,
                TagName = element.TagName.ToLowerInvariant(),
                Text = text,
                NodePath = BuildNodePath(element, root),
                SelectorHint = BuildSelectorHint(element)
            });
        }

        foreach (var element in section.QuerySelectorAll("a[href]"))
        {
            var href = element.GetAttribute("href") ?? string.Empty;
            var text = element.TextContent?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            model.Blocks.Add(new LpImportBlock
            {
                BlockId = $"{model.Id}-link-{blockIndex++}",
                Type = LpImportBlockType.Link,
                TagName = element.TagName.ToLowerInvariant(),
                Text = text,
                Href = href,
                NodePath = BuildNodePath(element, root),
                SelectorHint = BuildSelectorHint(element)
            });
        }

        foreach (var element in section.QuerySelectorAll("img[src]"))
        {
            var src = element.GetAttribute("src") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(src))
            {
                continue;
            }

            model.Blocks.Add(new LpImportBlock
            {
                BlockId = $"{model.Id}-image-{blockIndex++}",
                Type = LpImportBlockType.Image,
                TagName = element.TagName.ToLowerInvariant(),
                Src = src,
                Alt = element.GetAttribute("alt") ?? string.Empty,
                NodePath = BuildNodePath(element, root),
                SelectorHint = BuildSelectorHint(element),
                AssetRef = new LpImportAssetRef
                {
                    Path = src,
                    IsExternal = IsExternalUrl(src),
                    MimeType = GuessMimeType(src)
                }
            });
        }

        if (model.Blocks.Count == 0)
        {
            model.Blocks.Add(new LpImportBlock
            {
                BlockId = $"{model.Id}-frozen",
                Type = LpImportBlockType.Frozen,
                TagName = section.TagName.ToLowerInvariant(),
                FrozenReason = model.FrozenReasons.FirstOrDefault() ?? "未対応要素を含むため固定"
            });
        }
    }

    private static void ApplyEditsToDocument(IDocument document, LpImportProject project, bool logFailures)
    {
        project.ReplaceFailures.Clear();
        foreach (var section in project.Sections)
        {
            foreach (var block in section.Blocks.Where(block => block.Type != LpImportBlockType.Frozen))
            {
                if (document.Body is null)
                {
                    continue;
                }

                var element = TryFindElementByPath(document.Body, block.NodePath);
                if (element is null)
                {
                    block.HasReplaceError = true;
                    block.Type = LpImportBlockType.Frozen;
                    block.FrozenReason = "置換失敗";
                    if (logFailures)
                    {
                        project.ReplaceFailures.Add(new LpImportWarning
                        {
                            Code = "replace-failed",
                            Message = "差分適用に失敗",
                            Detail = block.SelectorHint
                        });
                    }
                    continue;
                }

                switch (block.Type)
                {
                    case LpImportBlockType.Text:
                        element.TextContent = block.Text;
                        break;
                    case LpImportBlockType.Link:
                        element.SetAttribute("href", block.Href);
                        if (!string.IsNullOrWhiteSpace(block.Text))
                        {
                            element.TextContent = block.Text;
                        }
                        break;
                    case LpImportBlockType.Image:
                        element.SetAttribute("src", block.Src);
                        if (!string.IsNullOrWhiteSpace(block.Alt))
                        {
                            element.SetAttribute("alt", block.Alt);
                        }
                        break;
                }
            }
        }
    }

    private static void InlineLocalAssetsForPreview(IDocument document, LpImportProject project)
    {
        var baseDir = GetBaseDirectory(project.EntryHtmlPath);

        foreach (var image in document.QuerySelectorAll("img[src]"))
        {
            var src = image.GetAttribute("src") ?? string.Empty;
            if (IsExternalUrl(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resolved = ResolveRelativePath(baseDir, src);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                continue;
            }

            if (!TryGetAssetBytes(project, resolved, out var data))
            {
                continue;
            }

            var mime = GuessMimeType(resolved);
            var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(data)}";
            image.SetAttribute("src", dataUrl);
        }

        foreach (var link in document.QuerySelectorAll("link[rel~='stylesheet'][href]"))
        {
            var href = link.GetAttribute("href") ?? string.Empty;
            if (IsExternalUrl(href))
            {
                continue;
            }

            var resolved = ResolveRelativePath(baseDir, href);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                continue;
            }

            if (!TryGetAssetBytes(project, resolved, out var data))
            {
                continue;
            }

            var css = Encoding.UTF8.GetString(data);
            var style = document.CreateElement("style");
            style.TextContent = css;
            link.Replace(style);
        }

        foreach (var script in document.QuerySelectorAll("script[src]"))
        {
            var src = script.GetAttribute("src") ?? string.Empty;
            if (IsExternalUrl(src))
            {
                continue;
            }

            var resolved = ResolveRelativePath(baseDir, src);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                continue;
            }

            if (!TryGetAssetBytes(project, resolved, out var data))
            {
                continue;
            }

            var js = Encoding.UTF8.GetString(data);
            var inline = document.CreateElement("script");
            inline.TextContent = js;
            script.Replace(inline);
        }
    }

    private static void RemoveTempAttributes(IDocument document)
    {
        foreach (var element in document.QuerySelectorAll("[data-lpe-id]"))
        {
            element.RemoveAttribute("data-lpe-id");
        }
    }

    private static bool TryGetAssetBytes(LpImportProject project, string path, out byte[] data)
    {
        if (project.AssetOverrides.TryGetValue(path, out data!))
        {
            return true;
        }

        if (project.NewAssets.TryGetValue(path, out data!))
        {
            return true;
        }

        if (project.Files.TryGetValue(path, out var file))
        {
            data = file.Data;
            return true;
        }

        data = Array.Empty<byte>();
        return false;
    }

    private static ImportStats BuildStats(LpImportProject project)
    {
        var stats = new ImportStats();
        foreach (var section in project.Sections)
        {
            if (section.IsFrozen)
            {
                stats.FrozenSectionCount++;
                foreach (var reason in section.FrozenReasons)
                {
                    if (!stats.FrozenReasonSamples.Contains(reason))
                    {
                        stats.FrozenReasonSamples.Add(reason);
                    }
                }
            }

            foreach (var block in section.Blocks)
            {
                switch (block.Type)
                {
                    case LpImportBlockType.Text:
                        stats.EditableTextCount++;
                        break;
                    case LpImportBlockType.Image:
                        stats.EditableImageCount++;
                        break;
                    case LpImportBlockType.Link:
                        stats.EditableLinkCount++;
                        break;
                    case LpImportBlockType.Frozen:
                        stats.FrozenBlockCount++;
                        if (!string.IsNullOrWhiteSpace(block.FrozenReason)
                            && !stats.FrozenReasonSamples.Contains(block.FrozenReason))
                        {
                            stats.FrozenReasonSamples.Add(block.FrozenReason);
                        }
                        break;
                }
            }
        }

        return stats;
    }

    private static string GetSectionTitle(IElement section, int index)
    {
        var heading = section.QuerySelector("h1,h2,h3");
        if (heading is not null && !string.IsNullOrWhiteSpace(heading.TextContent))
        {
            return heading.TextContent.Trim();
        }

        var label = section.GetAttribute("aria-label") ?? section.GetAttribute("data-title");
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label.Trim();
        }

        return $"セクション {index}";
    }

    private static List<string> DetectFrozenReasons(IElement section)
    {
        var reasons = new List<string>();
        if (section.QuerySelectorAll("iframe,canvas,video,script").Any())
        {
            reasons.Add("iframe/canvas/video/script");
        }

        return reasons;
    }

    private static List<int> BuildNodePath(IElement element, IElement? root)
    {
        var path = new List<int>();
        var current = element;
        while (current.ParentElement is not null && current != root)
        {
            var parent = current.ParentElement;
            var index = GetElementIndex(parent, current);
            if (index < 0)
            {
                break;
            }
            path.Add(index);
            current = parent;
        }

        path.Reverse();
        return path;
    }

    private static IElement? TryFindElementByPath(IElement root, IReadOnlyList<int> path)
    {
        var current = root;
        foreach (var index in path)
        {
            if (index < 0 || index >= current.Children.Length)
            {
                return null;
            }
            current = current.Children[index];
        }

        return current;
    }

    private static int GetElementIndex(IElement parent, IElement target)
    {
        var index = 0;
        foreach (var child in parent.Children)
        {
            if (ReferenceEquals(child, target))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    private static string BuildSelectorHint(IElement element)
    {
        var tag = element.TagName.ToLowerInvariant();
        var id = element.Id;
        var classes = element.ClassList.Length > 0
            ? string.Join(".", element.ClassList.Take(2))
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(id))
        {
            return $"{tag}#{id}";
        }
        if (!string.IsNullOrWhiteSpace(classes))
        {
            return $"{tag}.{classes}";
        }
        return tag;
    }

    private static string GuessMimeType(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private static bool IsHtmlFile(string path)
        => path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path) => path.Replace("\\", "/");

    private static bool IsExternalUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRelativePath(string baseDir, string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        var clean = rawPath.Split('#')[0].Split('?')[0];
        if (string.IsNullOrWhiteSpace(clean))
        {
            return string.Empty;
        }

        if (clean.StartsWith("/", StringComparison.Ordinal))
        {
            return clean.TrimStart('/');
        }

        var baseUri = new Uri($"file:///{baseDir}");
        var resolved = new Uri(baseUri, clean);
        return resolved.AbsolutePath.TrimStart('/');
    }

    private static string GetBaseDirectory(string entryHtmlPath)
    {
        var normalized = NormalizePath(entryHtmlPath);
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return string.Empty;
        }
        return normalized[..(lastSlash + 1)];
    }

    private static IEnumerable<string> ExtractCssUrls(string cssText)
    {
        if (string.IsNullOrWhiteSpace(cssText))
        {
            yield break;
        }

        foreach (Match match in Regex.Matches(cssText, "url\\(([^)]+)\\)", RegexOptions.IgnoreCase))
        {
            var value = match.Groups[1].Value.Trim('"', '\'', ' ');
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static string? DetectRootPrefix(ZipArchive archive)
    {
        var firstEntry = archive.Entries
            .Select(e => e.FullName.Replace("\\", "/"))
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstEntry))
        {
            return null;
        }

        var allSharePrefix = archive.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.FullName))
            .All(e => e.FullName.Replace("\\", "/").StartsWith(firstEntry + "/", StringComparison.OrdinalIgnoreCase));

        return allSharePrefix ? firstEntry + "/" : null;
    }

    private static void AddTextEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }

    private static void AddBinaryEntry(ZipArchive archive, string path, byte[] data)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }
}
