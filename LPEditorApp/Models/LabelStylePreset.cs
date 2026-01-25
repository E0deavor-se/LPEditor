namespace LPEditorApp.Models;

public sealed class LabelStylePreset
{
    public LabelStylePreset(string key, string name, string description, LabelStyleSnapshot style)
    {
        Key = key;
        Name = name;
        Description = description;
        Style = style;
    }

    public string Key { get; }
    public string Name { get; }
    public string Description { get; }
    public LabelStyleSnapshot Style { get; }
}
