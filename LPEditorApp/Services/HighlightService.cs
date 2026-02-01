using LPEditorApp.Models;

namespace LPEditorApp.Services;

public interface IHighlightService
{
    event Action<HighlightRequest>? OnRequested;
    bool Enabled { get; set; }
    void Trigger(HighlightRequest request);
}

public sealed class HighlightService : IHighlightService
{
    public event Action<HighlightRequest>? OnRequested;

    public bool Enabled { get; set; } = true;

    public void Trigger(HighlightRequest request)
    {
        if (!Enabled || request is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ScopeType))
        {
            request.ScopeType = "page";
        }

        if (request.DurationMs <= 0)
        {
            request.DurationMs = 320;
        }

        if (string.IsNullOrWhiteSpace(request.Style))
        {
            request.Style = "wash";
        }

        OnRequested?.Invoke(request);
    }
}
