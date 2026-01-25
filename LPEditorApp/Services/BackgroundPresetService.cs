using System.Text.Json;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public class BackgroundPresetService
{
    private readonly IWebHostEnvironment _environment;
    private IReadOnlyList<BackgroundPresetModel>? _cache;

    public BackgroundPresetService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<IReadOnlyList<BackgroundPresetModel>> GetPresetsAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var path = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "presets", "background-presets.json");
        if (!File.Exists(path))
        {
            _cache = Array.Empty<BackgroundPresetModel>();
            return _cache;
        }

        var json = await File.ReadAllTextAsync(path);
        var presets = JsonSerializer.Deserialize<List<BackgroundPresetModel>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<BackgroundPresetModel>();

        _cache = presets;
        return _cache;
    }
}
