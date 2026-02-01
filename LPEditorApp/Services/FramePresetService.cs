using System.Text.Json;
using LPEditorApp.Models;
using Microsoft.AspNetCore.Hosting;

namespace LPEditorApp.Services;

public sealed class FramePresetService
{
    private readonly IWebHostEnvironment _environment;
    private IReadOnlyList<FramePreset>? _cache;

    public FramePresetService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string? LastErrorMessage { get; private set; }

    public async Task<IReadOnlyList<FramePreset>> GetPresetsAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        try
        {
            var path = Path.Combine(_environment.ContentRootPath, "wwwroot", "presets", "frame-presets.json");
            if (!File.Exists(path))
            {
                LastErrorMessage = "プリセット定義が見つかりません。";
                _cache = Array.Empty<FramePreset>();
                return _cache;
            }

            var json = await File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var items = JsonSerializer.Deserialize<List<FramePreset>>(json, options) ?? new List<FramePreset>();
            _cache = items;
            LastErrorMessage = null;
            return _cache;
        }
        catch
        {
            LastErrorMessage = "プリセットの読み込みに失敗しました。";
            _cache = Array.Empty<FramePreset>();
            return _cache;
        }
    }
}
