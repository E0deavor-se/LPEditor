using System.ComponentModel.DataAnnotations;

namespace LPEditorApp.Models.Ai;

public class AiDesignRequest
{
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;

    [MaxLength(120)]
    public string CampaignType { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tone { get; set; } = "casual";

    [MaxLength(30)]
    public string BrandColorHint { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ReferenceUrl { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Prohibited { get; set; } = string.Empty;
}
