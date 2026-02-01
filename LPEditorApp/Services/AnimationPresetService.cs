using System.Text.Json;
using LPEditorApp.Models;
using Microsoft.AspNetCore.Hosting;

namespace LPEditorApp.Services;

public sealed class AnimationPresetService
{
    private readonly IWebHostEnvironment _environment;
    private IReadOnlyList<AnimationPreset>? _cache;

    public AnimationPresetService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string? LastErrorMessage { get; private set; }

    public async Task<IReadOnlyList<AnimationPreset>> GetPresetsAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        try
        {
            var path = Path.Combine(_environment.ContentRootPath, "wwwroot", "presets", "animation-presets.json");
            if (!File.Exists(path))
            {
                LastErrorMessage = "アニメーションプリセットが見つかりません。";
                _cache = Array.Empty<AnimationPreset>();
                return _cache;
            }

            var json = await File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<AnimationPreset>>(json, options) ?? new List<AnimationPreset>();
            _cache = items;
            LastErrorMessage = null;
            return _cache;
        }
        catch
        {
            LastErrorMessage = "アニメーションプリセットの読み込みに失敗しました。";
            _cache = Array.Empty<AnimationPreset>();
            return _cache;
        }
    }
}
