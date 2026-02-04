using System.ComponentModel.DataAnnotations;

namespace LPEditorApp.Models.Ai;

public class AiGenerateLpRequest
{
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;

    [MaxLength(120)]
    public string BrandName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CampaignOverview { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Offer { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Conditions { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Period { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Target { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tone { get; set; } = "casual";

    [MaxLength(30)]
    public string Goal { get; set; } = "acquisition";

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ProhibitedExpressions { get; set; } = string.Empty;

    [MaxLength(500)]
    public string RequiredStatements { get; set; } = string.Empty;
}
