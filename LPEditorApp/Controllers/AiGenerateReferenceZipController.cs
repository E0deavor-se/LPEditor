using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateReferenceZipController : ControllerBase
{
    private readonly AiGenerateReferenceZipService _service;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateReferenceZipController(AiGenerateReferenceZipService service, LPEditorApp.Utils.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-reference-zip")]
    public async Task<IActionResult> GenerateAsync([FromBody] AiReferenceDesignRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _service.GenerateAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.ZipBytes is not null)
        {
            var payload = new
            {
                zipBase64 = Convert.ToBase64String(outcome.ZipBytes),
                html = outcome.Html,
                css = outcome.Css
            };
            return Ok(payload);
        }

        _logger.Warn($"[AI-RefZip] failed: {string.Join(" | ", outcome.Errors)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }
}
