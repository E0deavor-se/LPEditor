using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class StepNavItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("isComplete")]
    public bool IsComplete { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}
