namespace LPEditorApp.Models;

public class TemplateFileInfo
{
    public string RelativePath { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public bool IsImage { get; set; }
    public bool IsHtml { get; set; }
    public bool IsJs { get; set; }
    public bool IsCss { get; set; }
    public bool IsJson { get; set; }
}

public class TemplateProject
{
    public string TemplateId { get; set; } = Guid.NewGuid().ToString();
    public string? SourceZipPath { get; set; }
    public Dictionary<string, TemplateFileInfo> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ContentModel CurrentContent { get; set; } = new();
    public List<string> SectionGroupKeys { get; set; } = new();

    public static bool IsExcludedPath(string relativePath)
    {
        var lower = relativePath.Replace("\\", "/").ToLowerInvariant();
        return lower.StartsWith(".vs/")
            || lower.StartsWith(".vscode/")
            || lower.StartsWith(".github/")
            || lower.StartsWith(".git/")
            || lower.StartsWith("node_modules/")
            || lower.Contains("/.vs/")
            || lower.Contains("/.vscode/")
            || lower.Contains("/.github/")
            || lower.Contains("/.git/")
            || lower.Contains("/node_modules/");
    }
}
