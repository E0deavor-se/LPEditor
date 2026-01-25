using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class SectionTextBinding
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("buttonText")]
    public string ButtonText { get; set; } = string.Empty;
}
