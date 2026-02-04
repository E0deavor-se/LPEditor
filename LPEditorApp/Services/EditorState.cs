using LPEditorApp.Models;
using LPEditorApp.Models.Ai;
using LPEditorApp.Utils;

namespace LPEditorApp.Services;

public class EditorState
{
    public TemplateProject? Template { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? TemplateLoadedAt { get; set; }
    public ContentModel Content { get; set; } = new();
    public Dictionary<string, byte[]> ImageOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ImageMeta> ImageMeta { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> DecoImagePaths { get; set; } = new();
    public List<string> BackgroundImagePaths { get; set; } = new();
    public List<string> OtherImagePaths { get; set; } = new();
    public string PreviewHtml { get; set; } = string.Empty;
    public LpDesignSpec? DesignSpec { get; set; }
    public LpDecorationSpec? DecorationSpec { get; set; }
    public LpReferenceStyleSpec? ReferenceStyleSpec { get; set; }
    public bool ForceAiDesign { get; set; }
    public string ExperimentalPreviewHtml { get; set; } = string.Empty;
    public LpBlueprint? LastBlueprint { get; set; }
    public AppErrorContext Error { get; } = new();
    public bool PreviewIsMobile { get; set; }
}
