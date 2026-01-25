using System.Text.Json;
using LPEditorApp.Models;

namespace LPEditorApp.Services;

public class ContentPersistService
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(string filePath, ContentModel content)
    {
        var json = JsonSerializer.Serialize(content, _options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<ContentModel> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ContentModel>(json) ?? new ContentModel();
    }
}
