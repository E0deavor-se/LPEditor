using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class SafetyIssue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("actionLabel")]
    public string ActionLabel { get; set; } = string.Empty;

    [JsonPropertyName("targetSection")]
    public string TargetSection { get; set; } = string.Empty;

    [JsonPropertyName("targetTab")]
    public string TargetTab { get; set; } = string.Empty;
}
