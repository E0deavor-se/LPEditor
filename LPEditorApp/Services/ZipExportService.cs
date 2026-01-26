using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LPEditorApp.Models;
using LPEditorApp.Utils;
using ILogger = LPEditorApp.Utils.ILogger;

namespace LPEditorApp.Services;

public class ZipExportService
{
    private readonly PreviewService _previewService;
    private readonly JsReplacementService _jsReplacementService;
    private readonly ImageService _imageService;
    private readonly ILogger _logger;

    public ZipExportService(
        PreviewService previewService,
        JsReplacementService jsReplacementService,
        ImageService imageService,
        ILogger logger)
    {
        _previewService = previewService;
        _jsReplacementService = jsReplacementService;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task ExportAsync(
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]> imageOverrides,
        string outputZipPath)
    {
        try
        {
            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            await using var zipStream = File.Create(outputZipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
            await BuildArchiveAsync(archive, template, content, imageOverrides);
            _logger.Info($"ZIP出力完了: {outputZipPath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"ZIP出力失敗: {ex.Message}");
            throw new ZipExportException("ZIP生成に失敗しました", ex);
        }
    }

    public async Task<byte[]> ExportBytesAsync(
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]> imageOverrides)
    {
        try
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                await BuildArchiveAsync(archive, template, content, imageOverrides);
            }

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error($"ZIP出力失敗: {ex.Message}");
            throw new ZipExportException("ZIP生成に失敗しました", ex);
        }
    }

    private async Task BuildArchiveAsync(
        ZipArchive archive,
        TemplateProject template,
        ContentModel content,
        IDictionary<string, byte[]> imageOverrides)
    {
        var html = await _previewService.GenerateHtmlAsync(template, content, imageOverrides, embedImages: false);
        html = _jsReplacementService.ReplaceCountdownEnd(html, content.Campaign.CountdownEnd);
        AddTextEntry(archive, "index.html", html);

        foreach (var file in template.Files.Values)
        {
            if (!TryNormalizeZipPath(file.RelativePath, out var path))
            {
                _logger.Warn($"ZIP出力: 無効なパスをスキップしました: {file.RelativePath}");
                continue;
            }
            if (content.DeletedImages.Any(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (TemplateProject.IsExcludedPath(path))
            {
                continue;
            }

            if (path.Equals("index.html", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.Equals("content.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsTargetCss(path))
            {
                var cssText = Encoding.UTF8.GetString(file.Data);
                cssText = EnsureEmphasisCss(cssText);
                AddTextEntry(archive, path, cssText);
                continue;
            }

            if (file.IsJs && path.StartsWith("shared/js/", StringComparison.OrdinalIgnoreCase))
            {
                var jsText = Encoding.UTF8.GetString(file.Data);
                var replaced = _jsReplacementService.ReplaceCountdownEnd(jsText, content.Campaign.CountdownEnd);
                AddTextEntry(archive, path, replaced);
                continue;
            }

            if (file.IsImage)
            {
                var data = file.Data;
                if (imageOverrides.TryGetValue(path, out var overrideBytes))
                {
                    data = await _imageService.ResizePngAsync(overrideBytes, GetMaxWidth(path, content));
                }

                AddBinaryEntry(archive, path, data);
                continue;
            }

            AddBinaryEntry(archive, path, file.Data);
        }

        foreach (var overridePair in imageOverrides)
        {
            if (!TryNormalizeZipPath(overridePair.Key, out var path))
            {
                _logger.Warn($"ZIP出力: 画像のパスが未設定のためスキップしました。");
                continue;
            }
            if (content.DeletedImages.Any(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            if (template.Files.ContainsKey(path))
            {
                continue;
            }

            var resized = await _imageService.ResizePngAsync(overridePair.Value, GetMaxWidth(path, content));
            AddBinaryEntry(archive, path, resized);
        }

        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true });
        AddTextEntry(archive, "content.json", json);
    }

    private static string NormalizePath(string path) => path.Replace("\\", "/");

    private static bool TryNormalizeZipPath(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var value = NormalizePath(path).Trim();
        value = value.TrimStart('/');
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Contains(":", StringComparison.Ordinal))
        {
            return false;
        }

        normalized = value;
        return true;
    }

    private static bool IsTargetCss(string path)
        => path.Equals("shared/css/site.css", StringComparison.OrdinalIgnoreCase);

    private static string EnsureEmphasisCss(string cssText)
    {
        var rule = ".is-emphasis { font-weight: 700; color: #d32f2f; }";
        return cssText.Contains(".is-emphasis", StringComparison.OrdinalIgnoreCase)
            ? cssText
            : cssText + Environment.NewLine + rule + Environment.NewLine;
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

    private static int GetMaxWidth(string path, ContentModel content)
    {
        if (path.Equals(content.Header.LogoImage, StringComparison.OrdinalIgnoreCase))
        {
            return 300;
        }

        if (path.Equals(content.Hero.ImagePc, StringComparison.OrdinalIgnoreCase))
        {
            return 1600;
        }

        if (path.Equals(content.Hero.ImageSp, StringComparison.OrdinalIgnoreCase))
        {
            return 900;
        }

        if (path.Equals(content.Sections.Conditions.TitleImage, StringComparison.OrdinalIgnoreCase))
        {
            return 1200;
        }

        if (path.Equals(content.Sections.Conditions.TextImage, StringComparison.OrdinalIgnoreCase))
        {
            return 1200;
        }

        if (path.Equals(content.Sections.Banners.Main.Image, StringComparison.OrdinalIgnoreCase))
        {
            return 1200;
        }

        if (path.Equals(content.Sections.Banners.Magazine.Image, StringComparison.OrdinalIgnoreCase))
        {
            return 1200;
        }

        return 1200;
    }
}
