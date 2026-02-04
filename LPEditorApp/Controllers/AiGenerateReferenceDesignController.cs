using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateReferenceDesignController : ControllerBase
{
    private readonly AiGenerateReferenceDesignService _service;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateReferenceDesignController(AiGenerateReferenceDesignService service, LPEditorApp.Utils.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-reference-design")]
    public async Task<IActionResult> GenerateAsync([FromBody] AiReferenceDesignRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _service.GenerateAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.Spec is not null)
        {
            return Ok(outcome.Spec);
        }

        _logger.Warn($"[AI-RefDesign] failed: {string.Join(" | ", outcome.Errors)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }
}
