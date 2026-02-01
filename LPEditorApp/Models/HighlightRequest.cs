using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public sealed class HighlightRequest
{
    [JsonPropertyName("scopeType")]
    public string ScopeType { get; set; } = "page";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("style")]
    public string Style { get; set; } = "wash";

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; } = 320;
}
