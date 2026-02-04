using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;
using Microsoft.AspNetCore.Mvc;

namespace LPEditorApp.Controllers;

[ApiController]
[Route("api/ai")]
public class AiGenerateLpController : ControllerBase
{
    private readonly AiGenerateLpService _aiService;
    private readonly LPEditorApp.Utils.ILogger _logger;

    public AiGenerateLpController(AiGenerateLpService aiService, LPEditorApp.Utils.ILogger logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    [HttpPost("generate-lp")]
    public async Task<IActionResult> GenerateLpAsync([FromBody] AiGenerateLpRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "入力が不正です。" });
        }

        var outcome = await _aiService.GenerateBlueprintAsync(request, cancellationToken);
        if (outcome.IsSuccess && outcome.Blueprint is not null)
        {
            return Ok(outcome.Blueprint);
        }

        _logger.Warn($"[AI] failed: {string.Join(" | ", outcome.Errors)}");
        _logger.Warn($"[AI] request: {MaskSensitive(request)}");
        return UnprocessableEntity(new { message = outcome.UserMessage ?? "AI生成に失敗しました。入力内容を見直して再度お試しください。" });
    }

    private static string MaskSensitive(AiGenerateLpRequest request)
    {
        var merged = $"{request.Industry} {request.BrandName} {request.CampaignOverview} {request.Offer} {request.Conditions} {request.Period} {request.Target} {request.Notes} {request.RequiredStatements} {request.ProhibitedExpressions}";
        return MaskSensitive(merged);
    }

    private static string MaskSensitive(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var masked = System.Text.RegularExpressions.Regex.Replace(raw, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "[email]");
        masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\d{2,4}-\d{2,4}-\d{3,4}", "[phone]");
        masked = System.Text.RegularExpressions.Regex.Replace(masked, @"\d{8,}", "[number]");
        return masked.Length > 800 ? masked[..800] + "..." : masked;
    }
}
