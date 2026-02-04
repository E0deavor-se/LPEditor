using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateZipController : ControllerBase
{
    private readonly AiGenerateZipService _service;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateZipController(AiGenerateZipService service, LPEditorApp.Utils.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-zip")]
    public async Task<IActionResult> GenerateZipAsync([FromBody] AiGenerateZipRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _service.GenerateZipAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.ZipBytes is not null)
        {
            var base64 = Convert.ToBase64String(outcome.ZipBytes);
            return Ok(new { zipBase64 = base64, html = outcome.Html, css = outcome.Css });
        }

        _logger.Warn($"[AI-Zip] failed: {string.Join(" | ", outcome.Errors)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }
}
