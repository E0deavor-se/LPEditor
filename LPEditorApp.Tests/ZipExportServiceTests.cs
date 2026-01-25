using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using LPEditorApp.Models;
using LPEditorApp.Services;
using LPEditorApp.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Xunit;

namespace LPEditorApp.Tests;

public class ZipExportServiceTests
{
    [Fact]
    public async Task ExportAsync_CreatesZipWithExpectedEntries()
    {
        var template = new TemplateProject();
        template.Files["index.html"] = new TemplateFileInfo
        {
            RelativePath = "index.html",
            IsHtml = true,
            Data = Encoding.UTF8.GetBytes("<html><head><title></title><meta name='description' /></head><body><section data-section='campaignContent'><h1 data-bind='campaignContent.title'></h1><ul data-bind='campaignContent.notes'></ul></section></body></html>")
        };
        template.Files["shared/js/main.js"] = new TemplateFileInfo
        {
            RelativePath = "shared/js/main.js",
            IsJs = true,
            Data = Encoding.UTF8.GetBytes("const endDate = new Date('2026-01-01T23:59:59');")
        };
        template.Files["shared/css/site.css"] = new TemplateFileInfo
        {
            RelativePath = "shared/css/site.css",
            IsCss = true,
            Data = Encoding.UTF8.GetBytes("body{color:#000;}")
        };
        template.Files["images/logo.png"] = new TemplateFileInfo
        {
            RelativePath = "images/logo.png",
            IsImage = true,
            Data = CreatePng()
        };

        var content = new ContentModel();
        content.Meta.PageTitle = "Test";
        content.Meta.Description = "Desc";
        content.Campaign.CountdownEnd = "2026-05-15T23:59:59";
        content.Sections.CampaignContent.Title = "Title";
        content.Sections.CampaignContent.Notes.Add(new TextItemModel { Text = "Note", Emphasis = true });

        var logger = new ConsoleLogger();
        var previewService = new PreviewService();
        var jsService = new JsReplacementService();
        var imageService = new ImageService(logger);
        var zipService = new ZipExportService(previewService, jsService, imageService, logger);

        var outputZip = Path.Combine(Path.GetTempPath(), $"lp-test-{Guid.NewGuid()}.zip");
        await zipService.ExportAsync(template, content, new Dictionary<string, byte[]>(), outputZip);

        using var archive = ZipFile.OpenRead(outputZip);
        Assert.NotNull(archive.GetEntry("index.html"));
        Assert.NotNull(archive.GetEntry("shared/js/main.js"));
        Assert.NotNull(archive.GetEntry("shared/css/site.css"));
        Assert.NotNull(archive.GetEntry("content.json"));

        using var jsStream = archive.GetEntry("shared/js/main.js")!.Open();
        using var jsReader = new StreamReader(jsStream);
        var jsText = await jsReader.ReadToEndAsync();
        Assert.Contains("2026-05-15T23:59:59", jsText);

        using var cssStream = archive.GetEntry("shared/css/site.css")!.Open();
        using var cssReader = new StreamReader(cssStream);
        var cssText = await cssReader.ReadToEndAsync();
        Assert.Contains(".is-emphasis", cssText);
    }

    private static byte[] CreatePng()
    {
        using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1, 1);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }
}
