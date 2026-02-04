using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateDecorController : ControllerBase
{
    private readonly AiGenerateDecorationService _service;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateDecorController(AiGenerateDecorationService service, LPEditorApp.Utils.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-decor")]
    public async Task<IActionResult> GenerateDecorAsync([FromBody] AiDesignRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _service.GenerateDecorationAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.Spec is not null)
        {
            return Ok(outcome.Spec);
        }

        _logger.Warn($"[AI-Decor] failed: {string.Join(" | ", outcome.Errors)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }
}
