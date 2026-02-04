using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateDesignController : ControllerBase
{
    private readonly AiGenerateDesignService _service;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateDesignController(AiGenerateDesignService service, LPEditorApp.Utils.ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-design")]
    public async Task<IActionResult> GenerateDesignAsync([FromBody] AiDesignRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _service.GenerateDesignAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.Spec is not null)
        {
            return Ok(outcome.Spec);
        }

        _logger.Warn($"[AI-Design] failed: {string.Join(" | ", outcome.Errors)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }
}
