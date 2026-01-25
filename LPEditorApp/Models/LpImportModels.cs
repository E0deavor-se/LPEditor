namespace LPEditorApp.Models;

public class LpImportSession
{
    public string SourceZipPath { get; set; } = string.Empty;
    public Dictionary<string, ImportedFile> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> HtmlEntries { get; } = new();
}

public class ImportedFile
{
    public string RelativePath { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public class LpImportProject
{
    public string SourceZipPath { get; set; } = string.Empty;
    public string EntryHtmlPath { get; set; } = string.Empty;
    public string EntryHtmlOriginal { get; set; } = string.Empty;
    public Dictionary<string, ImportedFile> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, byte[]> AssetOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, byte[]> NewAssets { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<LpImportSection> Sections { get; } = new();
    public List<LpImportWarning> Warnings { get; } = new();
    public List<LpImportWarning> MissingAssets { get; } = new();
    public List<LpImportWarning> ReplaceFailures { get; } = new();
    public ImportStats Stats { get; set; } = new();
    public bool IsReady { get; set; }
}

public class ImportStats
{
    public int EditableTextCount { get; set; }
    public int EditableImageCount { get; set; }
    public int EditableLinkCount { get; set; }
    public int FrozenSectionCount { get; set; }
    public int FrozenBlockCount { get; set; }
    public List<string> FrozenReasonSamples { get; } = new();
}

public class LpImportSection
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<int> NodePath { get; set; } = new();
    public string SelectorHint { get; set; } = string.Empty;
    public List<LpImportBlock> Blocks { get; } = new();
    public List<string> FrozenReasons { get; } = new();

    public bool IsFrozen => Blocks.Count == 0 || Blocks.All(block => block.Type == LpImportBlockType.Frozen);
}

public enum LpImportBlockType
{
    Text,
    Image,
    Link,
    Frozen
}

public class LpImportBlock
{
    public string BlockId { get; set; } = string.Empty;
    public LpImportBlockType Type { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public List<int> NodePath { get; set; } = new();
    public string SelectorHint { get; set; } = string.Empty;
    public LpImportAssetRef? AssetRef { get; set; }
    public string FrozenReason { get; set; } = string.Empty;
    public bool HasReplaceError { get; set; }
}

public class LpImportAssetRef
{
    public string Path { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public string MimeType { get; set; } = string.Empty;
}

public class LpImportWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
