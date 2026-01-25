namespace LPEditorApp.Models;

public sealed class FontFamilyOption
{
    public FontFamilyOption(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}
