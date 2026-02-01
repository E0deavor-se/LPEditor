using System.Text.Json;

namespace LPEditorApp.Services;

public sealed class TemplateRegistry
{
    private readonly IWebHostEnvironment _env;

    public TemplateRegistry(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<IReadOnlyList<TemplateCatalogItem>> GetTemplatesAsync()
    {
        var root = GetTemplatesRoot();
        if (!Directory.Exists(root))
        {
            return Array.Empty<TemplateCatalogItem>();
        }

        var results = new List<TemplateCatalogItem>();
        foreach (var dir in Directory.GetDirectories(root))
        {
            var id = Path.GetFileName(dir);
            var manifestPath = Path.Combine(dir, "template.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = await LoadManifestAsync(manifestPath);
            results.Add(new TemplateCatalogItem(
                id,
                manifest?.Name ?? id,
                manifest?.Description ?? string.Empty,
                manifest?.Tags ?? Array.Empty<string>(),
                manifest?.Thumbnail ?? string.Empty,
                manifest?.Version ?? "1.0.0",
                dir));
        }

        return results;
    }

    public async Task<TemplateCatalogItem?> GetTemplateAsync(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        var root = GetTemplateRoot(templateId);
        var manifestPath = Path.Combine(root, "template.json");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = await LoadManifestAsync(manifestPath);
        return new TemplateCatalogItem(
            templateId,
            manifest?.Name ?? templateId,
            manifest?.Description ?? string.Empty,
            manifest?.Tags ?? Array.Empty<string>(),
            manifest?.Thumbnail ?? string.Empty,
            manifest?.Version ?? "1.0.0",
            root);
    }

    public string GetTemplateRoot(string templateId)
    {
        return Path.Combine(GetTemplatesRoot(), templateId);
    }

    private string GetTemplatesRoot()
    {
        return Path.Combine(_env.WebRootPath ?? string.Empty, "templates");
    }

    private static async Task<TemplateManifest?> LoadManifestAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<TemplateManifest>(stream);
    }

    private sealed class TemplateManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Thumbnail { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
    }
}

public sealed record TemplateCatalogItem(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string Thumbnail,
    string Version,
    string RootPath);
